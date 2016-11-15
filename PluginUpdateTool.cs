/*
 * PluginUpdateTool
 * Copyright (c) 2016 Sebastian Southen & Samuel Warnock
 *
 */

using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PluginUpdateTool {
	class PluginUpdateTool {
		static void Main(string[] args) {
			List<string> plugins = new List<string>();
			bool makeBackup = false;
			bool doList = false;
			if (args.Length == 0)
				args = new string[] { "-h" };

			foreach (string arg in args) {
				if (arg.StartsWith("/") || arg.StartsWith("-")) {
					switch (arg.Substring(1)) {
						//case "u":
						case "d":
							makeBackup = true;
							break;
						case "l":
							doList = true;
							break;
						case "h":
						default:
							Console.WriteLine("Usage: {0} [options] <assembly>", System.AppDomain.CurrentDomain.FriendlyName);
							Console.WriteLine(" -d  Download and save existing assembly(s)");
							Console.WriteLine(" -l  List registered assemblies");
							Console.WriteLine(" -h  This help text");
							return;
					}
				}
				else {
					plugins.Add(arg);
				}
			}

			CrmConnection crmConnection = CrmConnection.Parse(Settings.Default.CRM);
			OrganizationService orgService = new OrganizationService(crmConnection);
			var query = new QueryExpression("pluginassembly") {
				//ColumnSet = new ColumnSet(true),
				ColumnSet = new ColumnSet("name", "version"),
				//Criteria = new FilterExpression(LogicalOperator.And) {
					//Conditions = {
					//	new ConditionExpression("name", ConditionOperator.Equal, name)
					//}
				//}
			};
			if (makeBackup) {
				query.ColumnSet.AddColumn("content");
			}
			if (plugins.Count > 0) {
				query.Criteria.FilterOperator = LogicalOperator.Or;
				foreach (string plugin in plugins) {
					query.Criteria.Conditions.Add(new ConditionExpression("name", ConditionOperator.Equal, plugin));
				}
			}
			List<Entity> results = orgService.RetrieveMultiple(query).Entities.ToList();

			if (doList) {
				foreach (var result in results.OrderBy(a => a["name"])) {
					Console.WriteLine("{0} {1}", result["name"], result["version"]);
				}
			}

			if (plugins.Count > 0) {
				if (makeBackup) {

				}
				var plugin = results.SingleOrDefault();// new Entity("pluginassembly");
				if (plugin == null)
					return;

				//plugin["content"] = System.Convert.ToBase64String(File.ReadAllBytes(name + ".dll"));
				orgService.Update(plugin);

			}

		}
	}
}
