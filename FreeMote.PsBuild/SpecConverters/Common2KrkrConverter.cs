﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using FreeMote.Psb;

namespace FreeMote.PsBuild.SpecConverters
{
    class Common2KrkrConverter : ISpecConverter
    {
        /// <summary>
        /// krkr uses <see cref="PsbPixelFormat.WinRGBA8"/> on windows platform
        /// </summary>
        public PsbPixelFormat TargetPixelFormat { get; set; } = PsbPixelFormat.WinRGBA8;
        public bool UseRL { get; set; } = true;

        public PsbSpec FromSpec { get; } = PsbSpec.win;
        public PsbSpec ToSpec { get; } = PsbSpec.krkr;

        public void Convert(PSB psb)
        {
            if (ConvertOption == SpecConvertOption.Minimum)
            {
                Remove(psb);
            }
            var iconInfo = TranslateResources(psb);
            Travel((PsbDictionary)psb.Objects["object"], iconInfo);
            if (ConvertOption == SpecConvertOption.Minimum)
            {
                Add(psb);
            }
            psb.Platform = PsbSpec.krkr;
        }

        public SpecConvertOption ConvertOption { get; set; } = SpecConvertOption.Default;

        private void Remove(PSB psb)
        {
            //Remove `easing`
            psb.Objects.Remove("easing");

            //Remove `/object/*/motion/*/bounds`
            //Remove `/object/*/motion/*/layerIndexMap`
            var obj = (PsbDictionary)psb.Objects["object"];
            foreach (var o in obj)
            {
                //var name = o.Key;
                foreach (var m in (PsbDictionary)((PsbDictionary)o.Value)["motion"])
                {
                    if (m.Value is PsbDictionary mDic)
                    {
                        mDic.Remove("bounds");
                        //mDic.Remove("layerIndexMap");
                    }
                }
            }
        }

        private Dictionary<string, List<string>> TranslateResources(PSB psb)
        {
            Dictionary<string, List<string>> iconInfos = new Dictionary<string, List<string>>();
            var source = (PsbDictionary)psb.Objects["source"];
            foreach (var tex in source)
            {
                if (tex.Value is PsbDictionary texDic)
                {
                    var iconList = new List<string>();
                    iconInfos.Add(tex.Key, iconList);
                    var bmps = TextureHelper.SplitTexture(texDic, psb.Platform);
                    var icons = (PsbDictionary)texDic["icon"];
                    foreach (var iconPair in icons)
                    {
                        iconList.Add(iconPair.Key);
                        var icon = (PsbDictionary)iconPair.Value;
                        var data = UseRL
                            ? RL.CompressImage(bmps[iconPair.Key], TargetPixelFormat)
                            : RL.GetPixelBytesFromImage(bmps[iconPair.Key], TargetPixelFormat);
                        icon["pixel"] =
                            new PsbResource { Data = data, Parents = new List<IPsbCollection>() { icon } };
                        icon["compress"] = UseRL ? new PsbString("RL") : new PsbString();
                        icon.Remove("left");
                        icon.Remove("top");
                    }

                    texDic.Remove("texture");
                    texDic["type"] = new PsbNumber(1);
                }
            }
            return iconInfos;
        }

        private void Travel(IPsbCollection collection, Dictionary<string, List<string>> iconInfo)
        {
            if (collection is PsbDictionary dic)
            {
                //mask+=1
                //add ox=0, oy=0
                //change src
                if (dic.ContainsKey("mask") && dic.GetName() == "content")
                {
                    if (dic["src"] is PsbString s)
                    {
                        //"blank" ("icon" : "32:32:16:16") -> "blank/32:32:16:16"
                        if (s.Value == "blank")
                        {
                            var size = dic["icon"].ToString();
                            dic["src"] = new PsbString($"blank/{size}");
                        }
                        //"tex" ("icon" : "0001") -> "src/tex/0001"
                        else if (iconInfo.ContainsKey(s))
                        {
                            var iconName = dic["icon"].ToString();
                            dic["src"] = new PsbString($"src/{s}/{iconName}");
                        }
                        else
                        {
                            var iconName = dic["icon"].ToString();
                            dic["src"] = new PsbString($"motion/{s}/{iconName}");
                        }
                    }

                    var num = (PsbNumber)dic["mask"];
                    num.IntValue = num.IntValue + 1;
                    //add src = layout || src = shape/point (0)
                    if (num.IntValue == 1 || num.IntValue == 3 || num.IntValue == 19)
                    {
                        if (!dic.ContainsKey("src"))
                        {
                            bool isLayout = true;
                            //content -> {} -> [] -> {}
                            if (dic.Parent.Parent.Parent is PsbDictionary childrenArrayDic)
                            {
                                if (childrenArrayDic.ContainsKey("shape"))
                                {
                                    string shape;
                                    switch (((PsbNumber)childrenArrayDic["shape"]).IntValue)
                                    {
                                        case 0: //We only know 0 = point
                                        default:
                                            shape = "point";
                                            break;
                                    }
                                    dic.Add("src", new PsbString($"shape/{shape}"));
                                    isLayout = false;
                                }
                            }
                            if (isLayout)
                            {
                                dic.Add("src", new PsbString("layout"));
                            }
                        }
                    }
                    if (!dic.ContainsKey("ox"))
                    {
                        dic.Add("ox", new PsbNumber(0));
                    }
                    if (!dic.ContainsKey("oy"))
                    {
                        dic.Add("oy", new PsbNumber(0));
                    }
                }

                foreach (var child in dic.Values)
                {
                    if (child is IPsbCollection childCol)
                    {
                        Travel(childCol, iconInfo);
                    }
                }
            }
            if (collection is PsbCollection col)
            {
                foreach (var child in col)
                {
                    if (child is IPsbCollection childCol)
                    {
                        Travel(childCol, iconInfo);
                    }
                }
            }
        }

        private void Add(PSB psb)
        {
            var metadata = (PsbDictionary)psb.Objects["metadata"];
            if (!metadata.ContainsKey("attrcomp"))
            {
                metadata.Add("attrcomp", new PsbDictionary(1));
            }
        }
    }
}