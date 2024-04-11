﻿using Fushigi.Byml.Serializer;
using Fushigi.SARC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.actor_pack.components
{
    [Serializable]
    public class ModelExpandParam
    {
        public void LoadParentIfExists(Func<string, ModelExpandParam> fileLoader)
        {
            if (ParentRef == null)
                return;

            Parent = fileLoader(ParentRef);
            Parent.LoadParentIfExists(fileLoader);
        }

        [BymlProperty(Key = "$parent")]
        public string? ParentRef { get; set; }

        public ModelExpandParam? Parent { get; set; }

        public List<ModelExpandParamSettings> Settings { get; set; }
    }

    [Serializable]
    public class ModelExpandParamSettings
    {
        [BymlProperty("ModelKeyName")]
        public string mModelKeyName { get; set; } = "";

        [BymlProperty("MinScale")]
        public Vector2 mMinScale { get; set; }

        [BymlProperty("BoneSetting")]
        public ModelExpandBoneSetting mBoneSetting { get; set; }

        [BymlProperty("MatSetting")]
        public ModelExpandMatSetting mMatSetting { get; set; }
    }

    [Serializable]
    public class ModelExpandBoneSetting
    {
        public List<ModelExpandBoneParam> BoneInfoList { get; set; }

        public bool IsUpdateByModelUpdateWldMtx { get; set; }
    }

    [Serializable]
    public class ModelExpandBoneParam
    {
        [BymlProperty("BoneName", DefaultValue = "")]
        public string mBoneName { get; set; }

        [BymlProperty("CalcType", DefaultValue = "")]
        public string mCalcType { get; set; }

        [BymlProperty("CalcType2", DefaultValue = "")]
        public string mCalcTypeB { get; set; }

        [BymlProperty("ScalingType", DefaultValue = "")]
        public string mScalingType { get; set; }

        [BymlProperty("CustomCalc")]
        public CustomCalc mCustomCalc { get; set; }

        [BymlProperty("CustomCalc2")]
        public CustomCalc mCustomCalcB { get; set; }

        [BymlProperty("IsCustomCalc")]
        public bool mIsCustomCalc { get; set; }

        public Vector3 CalculateScale()
        {
            return Vector3.One;
        }
    }

    [Serializable]
    public class ModelExpandMatSetting
    {
        public List<ModelExpandMatParam> MatInfoList { get; set; }
        public bool IsUpdateByModelUpdateWldMtx { get; set; }
    }

    [Serializable]
    public class ModelExpandMatParam
    {
        [BymlProperty("MatNameSuffix", DefaultValue = "")]
        public string mMatNameSuffix { get; set; }

        [BymlProperty("CalcType", DefaultValue = "")]
        public string mCalcType { get; set; }

        [BymlProperty("CalcType2", DefaultValue = "")]
        public string mCalcTypeB { get; set; }

        [BymlProperty("ScalingType", DefaultValue = "")]
        public string mScalingType { get; set; }

        [BymlProperty("CustomCalc")]
        public CustomCalc mCustomCalc { get; set; }

        [BymlProperty("CustomCalc2")]
        public CustomCalc mCustomCalcB { get; set; }

        [BymlProperty("IsCustomCalc")]
        public bool mIsCustomCalc { get; set; }

        public Vector3 CalculateScale()
        {
            return Vector3.One;
        }
    }

    [Serializable]
    public class CustomCalc
    {
        public float A {  get; set; }
        public float B { get; set; }
    }
}
