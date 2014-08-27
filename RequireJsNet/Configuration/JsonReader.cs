﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RequireJsNet.Models;

namespace RequireJsNet.Configuration
{
    internal class JsonReader : IConfigReader
    {
        private readonly string path;

        private readonly ConfigLoaderOptions options;

        public JsonReader(string path, ConfigLoaderOptions options)
        {
            this.path = path;
            this.options = options;
        }

        public string Path
        {
            get
            {
                return this.path;
            }
        }

        public ConfigurationCollection ReadConfig()
        {
            var text = File.ReadAllText(Path);

            var collection = new ConfigurationCollection();
            var deserialized = (JObject)JsonConvert.DeserializeObject(text);
            collection.FilePath = Path;
            collection.Paths = GetPaths(deserialized);
            collection.Shim = GetShim(deserialized);
            collection.Map = GetMap(deserialized);

            if (options.ProcessBundles)
            {
                collection.Bundles = GetBundles(deserialized);
            }

            collection.AutoBundles = GetAutoBundles(deserialized);
            collection.Overrides = GetOverrides(deserialized);


            return collection;
        }

        private RequirePaths GetPaths(JObject document)
        {
            var paths = new RequirePaths();
            paths.PathList = document["paths"].Select(
                r =>
                    {
                        var result = new RequirePath();
                        var prop = (JProperty)r;
                        result.Key = prop.Name;
                        if (prop.Value.Type == JTokenType.String)
                        {
                            result.Value = prop.Value.ToString();
                        }
                        else
                        {
                            var pathObj = (JObject)prop.Value;
                            result.Value = pathObj["path"].ToString();
                            result.DefaultBundle = pathObj["defaultBundle"].ToString();
                        }

                        return result;
                    }).ToList();
            return paths;
        }

        private RequireShim GetShim(JObject document)
        {
            var shim = new RequireShim();
            shim.ShimEntries = new List<ShimEntry>();
            if (document["shim"] != null)
            {
                shim.ShimEntries = document["shim"].Select(
                    r =>
                        {
                            var result = new ShimEntry();
                            var prop = (JProperty)r;
                            result.For = prop.Name;
                            var shimObj = (JObject)prop.Value;
                            result.Exports = shimObj["exports"] != null ? shimObj["exports"].ToString() : null;
                            var depArr = (JArray)shimObj["deps"];
                            if (depArr != null)
                            {
                                result.Dependencies = depArr.Select(dep => new RequireDependency
                                                                               {
                                                                                   Dependency = dep.ToString()
                                                                               })
                                                            .ToList();
                            }

                            return result;
                        })
                        .ToList();                
            }

            return shim;
        }

        private RequireBundles GetBundles(JObject document)
        {
            var bundles = new RequireBundles();
            bundles.BundleEntries = new List<RequireBundle>();
            if (document["bundles"] != null)
            {
                bundles.BundleEntries = document["bundles"].Select(
                    r =>
                        {
                            var bundle = new RequireBundle();
                            var prop = (JProperty)r;
                            bundle.Name = prop.Name;
                            if (prop.Value is JArray)
                            {
                                bundle.IsVirtual = false;
                                var arr = prop.Value as JArray;
                                bundle.BundleItems = arr.Select(x => new BundleItem { ModuleName = x.ToString() } ).ToList();    
                                
                            }
                            else if(prop.Value is JObject)
                            {
                                var obj = prop.Value as JObject;
                                bundle.OutputPath = obj["outputPath"] != null ? obj["outputPath"].ToString() : null;
                                var inclArr = obj["includes"] as JArray;
                                if (inclArr != null)
                                {
                                    bundle.Includes = inclArr.Select(x => x.ToString()).ToList();
                                }

                                var itemsArr = obj["items"] as JArray;
                                if (itemsArr != null)
                                {
                                    bundle.BundleItems = itemsArr.Select(
                                        x =>
                                            {
                                                var result = new BundleItem();
                                                if (x is JObject)
                                                {
                                                    result.ModuleName = x["path"] != null ? x["path"].ToString() : null;
                                                    result.CompressionType = x["compression"] != null ? x["compression"].ToString() : null;
                                                }
                                                else if(x is JValue && x.Type == JTokenType.String)
                                                {
                                                    result.ModuleName = x.ToString();
                                                }

                                                return result;
                                            }).ToList();
                                }

                                var isVirtual = obj["virtual"];
                                bundle.IsVirtual = isVirtual is JValue 
                                                    && isVirtual.Type == JTokenType.Boolean
                                                    && (bool)((JValue)isVirtual).Value;
                            }

                            return bundle;
                        })
                    .ToList();
            }

            return bundles;
        }

        private RequireMap GetMap(JObject document)
        {
            return new RequireMap
                       {
                           MapElements = new List<RequireMapElement>() 
                       };
        }

        private List<CollectionOverride> GetOverrides(JObject document)
        {
            return new List<CollectionOverride>();
        }

        private AutoBundles GetAutoBundles(JObject document)
        {
            return new AutoBundles{ Bundles = new List<AutoBundle>()};
        }
    }
}
