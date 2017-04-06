/*
 * AutoDeploy
 * Copyright (c) 2016-2017 Sebastian Southen & Samuel Warnock
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System.Activities;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.IO;
using System.Threading;
using System.Reflection;

namespace AutoDeploy
{
	class AssemBounce
	{
		public int counter;
		public string fname;
	}

	class Flag
	{
		bool state = false;
		string name;

		public Flag(string n, bool def = false)
		{
			name = n;
			state = def;
		}

		public static implicit operator bool(Flag f)
		{
			if (Program.flags.ContainsKey(f.name))
			{
				f.state = Program.flags[f.name];
			}
			return f.state;
		}
	}

	class Program
	{
		public static List<AssemBounce> debouncer = new List<AssemBounce>();
		public static Dictionary<string, bool> flags = new Dictionary<string, bool>();

		private static Flag reflectOnly = new Flag("reflect-only");

		static void Main(string[] args)
		{
			parseArgs(args);

			FileSystemWatcher watcher = new FileSystemWatcher();
			watcher.Path = Environment.CurrentDirectory;
			watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Filter = "*.dll";
			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.EnableRaisingEvents = true;

			if (reflectOnly)
				AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;

			while (true)
			{
				int sz = debouncer.Count;
				for (int i = 0; i < sz;)
				{
					var p = debouncer[i];
					p.counter--;
					if (p.counter <= 0)
					{
						Upload(p.fname);
						debouncer.RemoveAt(i);
					}
					else
					{
						i++;
					}
				}
				Thread.Sleep(100);
			}

			return;
		}

		class Arg
		{
			public string name;
			public int paramCount;
			public Action<string[]> action;
			public Arg(string n, int c = 0, Action<string[]> a = null)
			{
				name = n;
				paramCount = c;
				if (a == null)
					a = setFlag;
				else
					action = a;
			}

			private void setFlag(string[] obj)
			{
				if (obj.Length < 1)
					return;
				if (obj.Length == 1)
					flags[obj[1]] = true;
				else if (obj[2] == "0")
					flags[obj[1]] = false;
			}
		}

		static List<Arg> cmdLineArgs = new List<Arg>
		{
			new Arg("reflect-only"),
		};

		private static void parseArgs(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				var a = cmdLineArgs.Where(arg => args[i].Contains(arg.name)).FirstOrDefault();
				List<string> par = new List<string>();
				par.Add(args[i]);
				for (int j = 0; j < a.paramCount; j++)
				{
					i++;
					par.Add(args[i]);
				}
				a.action(par.ToArray());
			}
		}

		private static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
		{
			String resourceName = new AssemblyName(args.Name).Name + ".dll";
			Assembly asm = Assembly.GetExecutingAssembly();
			using (var stream = asm.GetManifestResourceStream(resourceName))
			{
				if (stream != null)
				{
					Byte[] assemblyData = new Byte[stream.Length];
					stream.Read(assemblyData, 0, assemblyData.Length);
					return Assembly.ReflectionOnlyLoad(assemblyData);
				}
			}
			if (File.Exists(resourceName))
			{
				return Assembly.ReflectionOnlyLoad(File.ReadAllBytes(resourceName));
			}
			//var qq = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName == asm.FullName)
			//if (qq != null)
			//	ret

			return null;
		}

		private static void OnChanged(object sender, FileSystemEventArgs e)
		{
			//Console.WriteLine(e.FullPath);
			//Console.WriteLine(e.Name);
			bool retry = true;
			bool hasThings = false;
			while (retry)
			{
				try
				{
					var assem = Assembly.Load(File.ReadAllBytes(e.FullPath));
					retry = false;
					var types = assem.GetExportedTypes().Where(tt => !tt.IsAbstract && tt.IsClass && !tt.IsInterface);
					foreach (var t in types)
					{
						if (t.GetInterface(typeof(IPlugin).FullName) != null || t.GetInterface("Microsoft.Crm.Sdk.IPlugin") != null) // typeof(IPlugin).IsAssignableFrom(t))
						{
							hasThings = true;
						}
						else if (t.IsSubclassOf(typeof(System.Workflow.ComponentModel.Activity)) || t.IsSubclassOf(typeof(Activity))) //typeof(CodeActivity).IsAssignableFrom(t))
						{
							hasThings = true;
						}
					}
				}
				catch (ReflectionTypeLoadException rtle)
				{
					retry = false;
					return;
				}
				catch (Exception)
				{
					throw;
				}
			}
			if (hasThings)
			{
				var q = debouncer.Where(d => d.fname == e.FullPath).FirstOrDefault();
				if (q == null)
				{
					debouncer.Add(new AssemBounce() { counter = 1, fname = e.FullPath });
					Console.WriteLine("Added: " + e.Name);
				}
			}
		}

		private static void Upload(string fname)
		{
			Console.WriteLine("uploading: " + fname);
			return;

			//string connectionString = ConfigurationManager.ConnectionStrings["CRMSource"].ConnectionString;
			//CrmServiceClient conn = new Microsoft.Xrm.Tooling.Connector.CrmServiceClient(connectionString);
			//var orgService = (IOrganizationService)conn.OrganizationWebProxyClient != null ? (IOrganizationService)conn.OrganizationWebProxyClient : (IOrganizationService)conn.OrganizationServiceProxy;
			//CrmConnection crmConnection = CrmConnection.Parse(ConfigurationManager.ConnectionStrings["CRM"].ConnectionString);
			//OrganizationService orgService = new OrganizationService(crmConnection);
			CrmServiceClient orgService = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRM"].ConnectionString);

			string name = "Recurring Workflow";

			var qqw = orgService.RetrieveMultiple(new QueryExpression("pluginassembly")
			{
				ColumnSet = new ColumnSet("name"),
				Criteria = new FilterExpression(LogicalOperator.And)
				{
					Conditions =
					{
						new ConditionExpression("name", ConditionOperator.Equal, name)
					}
				}
			}).Entities.ToList();

			foreach (var q in qqw)
			{
				Console.WriteLine(q["name"] + ": " + q.Id);
			}

			var plugin = qqw.SingleOrDefault();// new Entity("pluginassembly");
			if (plugin == null)
				return;
			plugin["content"] = System.Convert.ToBase64String(File.ReadAllBytes(name + ".dll"));
			orgService.Update(plugin);

			return;
		}
	}
}
