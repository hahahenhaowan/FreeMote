﻿using System;
using System.Collections.Generic;
using FreeMote.Psb;

namespace FreeMote.PsBuild.Converters
{
    /// <summary>
    /// Common/Ems-Win Converter
    /// </summary>
    class CommonWinConverter : ISpecConverter
    {
        /// <summary>
        /// Won't be used in this conversion
        /// </summary>
        public SpecConvertOption ConvertOption { get; set; }
        /// <summary>
        /// Won't be used in this conversion
        /// </summary>
        public PsbPixelFormat TargetPixelFormat { get; set; }
        public bool UseRL { get; set; } = false;
        /// <summary>
        /// If true, it is an EmsWinConverter
        /// </summary>
        public bool EmsAsCommon { get; set; } = false;
        public IList<PsbSpec> FromSpec { get; } = new List<PsbSpec> {PsbSpec.win, PsbSpec.common};
        public IList<PsbSpec> ToSpec { get; } = new List<PsbSpec> {PsbSpec.krkr, PsbSpec.win};
        public void Convert(PSB psb)
        {
            if (!FromSpec.Contains(psb.Platform))
            {
                throw new FormatException("Can not convert Spec for this PSB");
            }

            var asSpec = EmsAsCommon ? PsbSpec.ems : PsbSpec.common;
            var toSpec = psb.Platform == PsbSpec.win ? asSpec : PsbSpec.win;
            var toPixelFormat = toSpec == asSpec ? PsbPixelFormat.CommonRGBA8 : PsbPixelFormat.WinRGBA8;
            var resList = psb.CollectResources(false);
            foreach (var resMd in resList)
            {
                var resourceData = resMd.Resource.Data;
                if (resourceData == null)
                {
                    continue;
                }
                if (resMd.Compress == PsbCompressType.RL)
                {
                    resourceData = RL.Uncompress(resourceData);
                }
                if (resMd.PixelFormat == PsbPixelFormat.DXT5)
                {
                    resourceData = RL.GetPixelBytesFromImage(
                        DxtUtil.Dxt5Decode(resourceData, resMd.Width, resMd.Height),toPixelFormat);
                    resMd.TypeString.Value = toPixelFormat.ToStringForPsb();
                }
                else
                {
                    RL.Rgba2Argb(ref resourceData);
                    if (UseRL)
                    {
                        resourceData = RL.Compress(resourceData);
                    }
                }
                resMd.Resource.Data = resourceData;
            }
            psb.Platform = toSpec;
        }
    }
}
