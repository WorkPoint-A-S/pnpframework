﻿using Microsoft.SharePoint.Client;
using PnP.Framework.Attributes;
using PnP.Framework.Provisioning.ObjectHandlers.TokenDefinitions;
using System;
using System.Text.RegularExpressions;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.TokenDefinitions
{
    [TokenDefinitionDescription(
      Token = "{termsetid:[groupname]:[termsetname]}",
      Description = "Returns the id of a term set given its name and its parent group",
      Example = "{termsetid:MyGroup:MyTermset}",
      Returns = "9188a794-cfcf-48b6-9ac5-df2048e8aa5d")]
    internal class TermIdToken : TokenDefinition
    {
        private readonly string _value = null;
        public TermIdToken(Web web, string groupName, string termsetName, string termPath, Guid id)
            : base(web, $"{{termid:{Regex.Escape(groupName)}:{Regex.Escape(termsetName)}:{Regex.Escape(termPath)}}}")
        {
            _value = id.ToString();
        }

        public override string GetReplaceValue()
        {
            if (string.IsNullOrEmpty(CacheValue))
            {
                CacheValue = _value;
            }
            return CacheValue;
        }
    }
}