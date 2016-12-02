namespace Sitecore.Support.XA.Feature.Composites.Pipelines.GetXmlBasedLayoutDefinition
{
    using Sitecore;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Mvc.Pipelines.Response.GetXmlBasedLayoutDefinition;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Xml.Linq;

    public class InjectCompositeComponents : Sitecore.XA.Feature.Composites.Pipelines.GetXmlBasedLayoutDefinition.InjectCompositeComponents
    {
        protected override void ProcessCompositeComponent(GetXmlBasedLayoutDefinitionArgs args, XElement rendering, XElement layoutXml)
        {
            string parentPh = rendering.Attribute("ph").Value;
            XAttribute attribute = rendering.Attribute("ds");
            if (string.IsNullOrEmpty(attribute?.Value))
            {
                Log.Warn("Composite component datasource is empty", rendering);
            }
            else
            {
                #region Fix for 4549
                Item datasource = args.CustomData["sxa-composite"] != null ?
                            this.ResolveCompositeDatasource(attribute.Value.Replace("local:", ((Sitecore.Data.Items.Item)args.CustomData["sxa-composite"]).Paths.Path)) :
                            this.ResolveCompositeDatasource(attribute.Value); // Added a check for args.CustomData["sxa-composite"] propertry. It contains a full path in case of nested composites. Replacing the "local:" part with the actual path fixes the issue.
                #endregion
                if (datasource == null)
                {
                    Log.Error("Composite component has a reference to non-existing datasource : " + attribute.Value, this);
                }
                else
                {
                    bool flag = this.DetectDatasourceLoop(args, datasource);
                    string str2 = HttpUtility.ParseQueryString(rendering.Attribute("par").Value)["DynamicPlaceholderId"];
                    int @int = MainUtil.GetInt(str2, 1);
                    string partialDesignId = string.Empty;
                    if (rendering.Attribute("sid") != null)
                    {
                        partialDesignId = rendering.Attribute("sid").Value;
                    }
                    if (!flag)
                    {
                        List<KeyValuePair<int, Item>> list = datasource.Children.Select<Item, KeyValuePair<int, Item>>(((Func<Item, int, KeyValuePair<int, Item>>)((item, idx) => new KeyValuePair<int, Item>(idx + 1, item)))).ToList<KeyValuePair<int, Item>>();
                        foreach (KeyValuePair<int, Item> pair in list)
                        {
                            if (!this.TryMergeComposites(args, rendering, layoutXml, pair, @int, parentPh, partialDesignId))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        this.AbortRecursivePipeline(args, rendering);
                    }
                    this.RollbackAntiLoopCollection(args, datasource);
                }
            }
        }
    }
}