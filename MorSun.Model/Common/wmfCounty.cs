﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HOHO18.Common;
using System.Data.Linq;

namespace MorSun.Model
{
    public partial class wmfCounty:IModel
    {
        #region Extensibility Method Definitions
        partial void OnLoaded();
        partial void OnValidate(System.Data.Linq.ChangeAction action);
        partial void OnCreated();
        partial void OnParentIdChanging(Guid value);
        #endregion

        public string CheckedId { get; set; }
        public Guid ProvinceId { get; set; }
        public bool IsValid
        {
            get { return (GetRuleViolations().Count() == 0); }
        }

        public IEnumerable<RuleViolation> GetRuleViolations()
        {
            ParameterProcess.TrimParameter<wmfCounty>(this);

            if (string.IsNullOrEmpty(CountyName))
                yield return new RuleViolation(XmlHelper.GetKeyNameValidation<wmfProvince>("县名不能为空"), "CountyName");
            if (!String.IsNullOrEmpty(CountyName) && ModelStateValidate.IsNotEmpty(CountyName.ToString()) && CountyName.ToString().Length > 15)
                yield return new RuleViolation(XmlHelper.GetKeyNameValidation<wmfReference>("县名长度不可大于15个字符"), "CountyName");

            yield break;
        }

        partial void OnValidate(ChangeAction action)
        {
            if (!IsValid)
                throw new ApplicationException("Rule violations prevent saving");
        }
    }
}
