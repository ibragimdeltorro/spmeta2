﻿using System;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPMeta2.Containers.Assertion;
using SPMeta2.Definitions;
using SPMeta2.Definitions.Base;

using SPMeta2.SSOM.Extensions;
using SPMeta2.SSOM.ModelHandlers;
using SPMeta2.Utils;
using SPMeta2.SSOM.ModelHosts;


namespace SPMeta2.Regression.SSOM.Validation
{
    public class ListViewDefinitionValidator : ListViewModelHandler
    {
        public override void DeployModel(object modelHost, DefinitionBase model)
        {
            var listModelHost = modelHost.WithAssertAndCast<ListModelHost>("modelHost", value => value.RequireNotNull());
            var definition = model.WithAssertAndCast<ListViewDefinition>("model", value => value.RequireNotNull());

            var list = listModelHost.HostList;
            var spObject = list.Views.FindByName(definition.Title);

            var assert = ServiceFactory.AssertService
                           .NewAssert(definition, spObject)
                               .ShouldBeEqual(m => m.Title, o => o.Title)
                               .ShouldBeEqual(m => m.IsDefault, o => o.IsDefaul())
                               .ShouldBeEqual(m => m.Query, o => o.Query)
                               .ShouldBeEqual(m => m.RowLimit, o => (int)o.RowLimit)
                               .ShouldBeEqual(m => m.IsPaged, o => o.Paged);

            if (string.IsNullOrEmpty(definition.Url))
                assert.SkipProperty(m => m.Url, "Url is null or empty. Skipping.");
            else
                assert.ShouldBePartOf(m => m.Url, o => o.ServerRelativeUrl);

            assert.ShouldBePartOf(m => m.JSLink, o => o.JSLink);

            assert.ShouldBeEqual((p, s, d) =>
            {
                var srcProp = s.GetExpressionValue(def => def.Fields);
                var dstProp = d.GetExpressionValue(ct => ct.ViewFields);

                var hasAllFields = true;

                foreach (var srcField in s.Fields)
                {
                    var listField = d.ParentList.Fields.OfType<SPField>().FirstOrDefault(f => f.StaticName == srcField);

                    // if list-scoped field we need to check by internal name
                    // internal name is changed for list scoped-fields
                    // that's why to check by BOTH, definition AND real internal name

                    if (!d.ViewFields.ToStringCollection().Contains(srcField) &&
                        !d.ViewFields.ToStringCollection().Contains(listField.InternalName))
                        hasAllFields = false;
                }

                return new PropertyValidationResult
                {
                    Tag = p.Tag,
                    Src = srcProp,
                    Dst = dstProp,
                    IsValid = hasAllFields
                };
            });
        }
    }

    internal static class ViewDefault
    {
        public static bool IsDefaul(this SPView view)
        {
            return view.ParentList.DefaultView.ID == view.ID;
        }
    }
}
