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
		[Flags]
		public enum Mode {
			None = 0,
			List = 1,
			Download = 2,
			Upload = 4
		}

		static void Main(string[] args) {
			List<string> plugins = new List<string>();
			Mode mode = Mode.None;

			if (args.Length == 0)
				args = new string[] { "-h" };

			foreach (string arg in args) {
				if (arg.StartsWith("/") || arg.StartsWith("-")) {
					switch (arg.Substring(1)) {
						case "l":
							mode |= Mode.List;
							break;
						case "d":
							mode |= Mode.Download;
							break;
						case "u":
							mode |= Mode.Upload;
							break;
						case "h":
						default:
							Console.WriteLine("Usage: {0} [options] <assembly>", System.AppDomain.CurrentDomain.FriendlyName);
							Console.WriteLine(" -d  Download and save existing assembly(s)");
							Console.WriteLine(" -l  List registered assemblies");
							Console.WriteLine(" -u  Upload and replace registered assembly(s)");
							Console.WriteLine(" -h  This help text");
							return;
					}
				}
				else {
					if (arg.EndsWith(".dll"))
						plugins.Add(arg.Remove(arg.Length - 4));
					else
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
			if (mode.HasFlag(Mode.Download)) {
				query.ColumnSet.AddColumn("content");
			}
			if (plugins.Count > 0) {
				query.Criteria.FilterOperator = LogicalOperator.Or;
				foreach (string plugin in plugins) {
					query.Criteria.Conditions.Add(new ConditionExpression("name", ConditionOperator.Equal, plugin));
				}
			}
			List<Entity> results = orgService.RetrieveMultiple(query).Entities.ToList();

			if (mode.HasFlag(Mode.List)) {
				foreach (var result in results.OrderBy(a => a["name"])) {
					Console.WriteLine("{0} v{1}", result["name"], result["version"]);	// Microsoft.Dynamics.FieldService 1.0.0.0
				}
				Console.WriteLine();
			}

			if (mode.HasFlag(Mode.Download)) {
				foreach (var result in results.OrderBy(a => a["name"])) {
					Console.WriteLine("Saving {0} v{1}....", result["name"], result["version"]);
					File.WriteAllBytes(
						result.GetAttributeValue<string>("name") + "." + result.GetAttributeValue<string>("version") + ".dll",
						System.Convert.FromBase64String(result.GetAttributeValue<string>("content"))
					);
				}
				Console.WriteLine();
			}

			if (plugins.Count > 0) {
				if (mode.HasFlag(Mode.Upload)) {
					foreach (var result in results.OrderBy(a => a["name"])) {
						if (plugins.Contains(result.GetAttributeValue<string>("name"))) {
							Console.WriteLine("Replacing {0} v{1}....", result["name"], result["version"]);
							result["content"] = System.Convert.ToBase64String(File.ReadAllBytes(result.GetAttributeValue<string>("name") + ".dll"));
							orgService.Update(result);
						}
					}
					Console.WriteLine();
				}
			}

#if DEBUG
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
#endif
		}
	}
}
