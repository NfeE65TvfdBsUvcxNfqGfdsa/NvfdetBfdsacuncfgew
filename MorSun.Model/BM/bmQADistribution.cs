﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HOHO18.Common;
using System.Data.Linq;
using System.ComponentModel.DataAnnotations;

namespace MorSun.Model
{
    [MetadataType(typeof(bmQADistributionMetadata))]
    public partial class bmQADistribution : IModel
    {
        #region Extensibility Method Definitions
        partial void OnLoaded();
        partial void OnValidate(System.Data.Linq.ChangeAction action);
        partial void OnCreated();
        partial void OnParentIdChanging(Guid value);
        #endregion

        public string CheckedId { get; set; }

        /// <summary>
        /// 待解答数量，用来排序
        /// </summary>
        public int DJDCount { get; set; }

        /// <summary>
        /// 活跃次数
        /// </summary>
        public int ActiveNum { get; set; }

        public bool IsValid
        {
            get { return (GetRuleViolations().Count() == 0); }
        }

        public IEnumerable<RuleViolation> GetRuleViolations()
        {
            ParameterProcess.TrimParameter<bmQADistribution>(this);            
            yield break;
        }

        partial void OnValidate(ChangeAction action)
        {
            if (!IsValid)
                throw new ApplicationException("Rule violations prevent saving");
        }
    }


    public class bmQADistributionMetadata
    {
        /// <summary>
        /// 待解答数量，用来排序
        /// </summary>
        public int DJDCount { get; set; }    
    }

}
