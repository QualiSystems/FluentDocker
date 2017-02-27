using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using Ductus.FluentDockerTest.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ductus.FluentDockerTest.FluentApiTests
{
    [TestClass]
    public class FluentContainerBasicTests
    {
        [TestMethod]
        public void BuildContainerRenderServiceInStoppedMode()
        {
            using (
                var container =
                    new Builder().UseContainer()
                        .UseImage("quali/monosible:v1")
                        //.WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                        .Build())
            {
                Assert.AreEqual(ServiceRunningState.Stopped, container.State);
            }
        }

        [TestMethod]
        public void BuildAndStartContainerWithKeepContainerWillLeaveContainerInArchve()
        {
            string id;
            var qualiServerIp = "192.168.30.87";

            //using (
            var container =
                new Builder().UseContainer()
                    .WithName("AnsibleContainer")
                    .UseImage("quali/monosible:v1")
                    .KeepContainer() //Do not delete on dispose
                    .KeepRunning() // Set Ineractive & Do not stop after create
                    .Build()
                    .Start()
                    .CopyTo("/", @"C:\Work\Trunk\Drop\CloudShell\Runtime", true)
                    .CopyTo("/", @"C:\Work\Trunk\Drop\TestShell\ExecutionServer", true)
                    .ExecuteCommands(new[]
                    {
                        new ExecCommand{Command = "cd /Runtime && export MONO_IOMAP=all* && mono QsRegisterRuntime.exe",Args = "bash -c"},
                        new ExecCommand{Command =$"cd /ExecutionServer && mono QsExecutionServerConsoleConfig.exe /s:{qualiServerIp} /u:admin /p:admin /esn:AnsibleTest /ansible",Args = "bash -c"},
                        new ExecCommand {Command = "cd /ExecutionServer && chmod +x ./ex", Args = "bash -c"},
                        new ExecCommand {Command = "cd /ExecutionServer && ./ex", Options = "-d",Args = "bash -c"}
                    });

            //.ExecuteCommand("cd /Runtime && export MONO_IOMAP=all* && mono QsRegisterRuntime.exe",
            //    args: "bash -c", throwOnError: true)
            //.ExecuteCommand(
            //    $"cd /ExecutionServer && mono QsExecutionServerConsoleConfig.exe /s:{qualiServerIp} /u:admin /p:admin /esn:AnsibleTest /ansible",
            //    args: "bash -c", throwOnError: true)
            //.ExecuteCommand("cd /ExecutionServer && chmod +x ./ex", args: "bash -c", throwOnError: true)
            //.ExecuteCommand("cd /ExecutionServer && ./ex", "-d", "bash -c", true);
            //)
            //{
            id = container.Id;
            Assert.IsNotNull(id);

            container.Remove();
            //}

            var image =
                new Hosts()
                    .Discover()
                    .Select(host => host.GetImages().FirstOrDefault(x => x.Name == "quali/monosible")).First();

            if (image != null) image.Remove();

            #region NA

            //// We shall have the container as stopped by now.
            //var cont =
            //  new Hosts()
            //    .Discover()
            //    .Select(host => host.GetContainers().FirstOrDefault(x => x.Id == id))
            //    .FirstOrDefault(container => null != container);

            //Assert.IsNotNull(cont);

            //cont.Remove(true);

            #endregion
        }

        [TestMethod]
        public void BuildAndStartContainerWithCustomEnvironmentWillBeReflectedInGetConfiguration()
        {
            using (
                var container =
                    new Builder().UseContainer()
                        .UseImage("kiasaki/alpine-postgres")
                        .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                        .Build()
                        .Start())
            {
                var config = container.GetConfiguration();

                Assert.AreEqual(ServiceRunningState.Running, container.State);
                Assert.IsTrue(config.Config.Env.Any(x => x == "POSTGRES_PASSWORD=mysecretpassword"));
            }
        }

        [TestMethod]
        public void ExplicitPortMappingShouldWork()
        {
            using (
                var container =
                    new Builder().UseContainer()
                        .UseImage("kiasaki/alpine-postgres")
                        .ExposePort(40001, 5432)
                        .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                        .Build()
                        .Start())
            {
                var endpoint = container.ToHostExposedEndpoint("5432/tcp");
                Assert.AreEqual(40001, endpoint.Port);
            }
        }

        [TestMethod]
        public void ImplicitPortMappingShouldWork()
        {
            using (
                var container =
                    new Builder().UseContainer()
                        .UseImage("kiasaki/alpine-postgres")
                        .ExposePort(5432)
                        .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                        .Build()
                        .Start())
            {
                var endpoint = container.ToHostExposedEndpoint("5432/tcp");
                Assert.AreNotEqual(0, endpoint.Port);
            }
        }

        [TestMethod]
        public void WaitForPortShallWork()
        {
            using (
                var container =
                    new Builder().UseContainer()
                        .UseImage("kiasaki/alpine-postgres")
                        .ExposePort(5432)
                        .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                        .WaitForPort("5432/tcp", 30000 /*30s*/)
                        .Build()
                        .Start())
            {
                var config = container.GetConfiguration(true);
                Assert.AreEqual(ServiceRunningState.Running, config.State.ToServiceState());
            }
        }

        [TestMethod]
        public void WaitForProcessShallWork()
        {
            using (
                var container =
                    new Builder().UseContainer()
                        .UseImage("kiasaki/alpine-postgres")
                        .ExposePort(5432)
                        .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                        .WaitForProcess("postgres", 30000 /*30s*/)
                        .Build()
                        .Start())
            {
                var config = container.GetConfiguration(true);
                Assert.AreEqual(ServiceRunningState.Running, config.State.ToServiceState());
            }
        }

        [TestMethod]
        public void VolumeMappingShallWork()
        {
            const string html = "<html><head>Hello World</head><body><h1>Hello world</h1></body></html>";
            var hostPath = (TemplateString)@"${TEMP}\fluentdockertest\${RND}";
            Directory.CreateDirectory(hostPath);

            using (
                var container =
                    new Builder().UseContainer()
                        .UseImage("nginx:latest")
                        .ExposePort(80)
                        .Mount(hostPath, "/usr/share/nginx/html", MountType.ReadOnly)
                        .Build()
                        .Start()
                        .WaitForPort("80/tcp", 30000 /*30s*/))
            {
                Assert.AreEqual(ServiceRunningState.Running, container.State);

                try
                {
                    File.WriteAllText(Path.Combine(hostPath, "hello.html"), html);

                    var response = $"http://{container.ToHostExposedEndpoint("80/tcp")}/hello.html".Wget();
                    Assert.AreEqual(html, response);
                }
                finally
                {
                    if (Directory.Exists(hostPath))
                        Directory.Delete(hostPath, true);
                }
            }
        }

        [TestMethod]
        public void CopyFromRunningContainerShallWork()
        {
            var fullPath = (TemplateString)@"${TEMP}\fluentdockertest\${RND}";
            Directory.CreateDirectory(fullPath);
            try
            {
                using (new Builder().UseContainer()
                    .UseImage("kiasaki/alpine-postgres")
                    .ExposePort(5432)
                    .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                    .Build()
                    .Start()
                    .CopyFrom("/etc/conf.d", fullPath))
                {
                    var files = Directory.EnumerateFiles(Path.Combine(fullPath, "conf.d")).ToArray();
                    Assert.IsTrue(files.Any(x => x.EndsWith("pg-restore")));
                    Assert.IsTrue(files.Any(x => x.EndsWith("postgresql")));
                }
            }
            finally
            {
                if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, true);
            }
        }

        [TestMethod]
        public void CopyBeforeDisposeContainerShallWork()
        {
            var fullPath = (TemplateString)@"${TEMP}\fluentdockertest\${RND}";
            Directory.CreateDirectory(fullPath);
            try
            {
                using (new Builder().UseContainer()
                    .UseImage("kiasaki/alpine-postgres")
                    .ExposePort(5432)
                    .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                    .CopyOnDispose("/etc/conf.d", fullPath)
                    .Build()
                    .Start())
                {
                }

                var files = Directory.EnumerateFiles(Path.Combine(fullPath, "conf.d")).ToArray();
                Assert.IsTrue(files.Any(x => x.EndsWith("pg-restore")));
                Assert.IsTrue(files.Any(x => x.EndsWith("postgresql")));
            }
            finally
            {
                if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, true);
            }
        }

        [TestMethod]
        public void ExportToTarFileWhenDisposeShallWork()
        {
            var fullPath = (TemplateString)@"${TEMP}\fluentdockertest\${RND}\export.tar";
            // ReSharper disable once AssignNullToNotNullAttribute
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            try
            {
                using (new Builder().UseContainer()
                    .UseImage("kiasaki/alpine-postgres")
                    .ExposePort(5432)
                    .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                    .ExportOnDispose(fullPath)
                    .Build()
                    .Start())
                {
                }

                Assert.IsTrue(File.Exists(fullPath));
            }
            finally
            {
                if (File.Exists(fullPath))
                    Directory.Delete(Path.GetDirectoryName(fullPath), true);
            }
        }

        [TestMethod]
        public void ExportExploadedWhenDisposeShallWork()
        {
            var fullPath = (TemplateString)@"${TEMP}\fluentdockertest\${RND}";
            Directory.CreateDirectory(fullPath);
            try
            {
                using (new Builder().UseContainer()
                    .UseImage("kiasaki/alpine-postgres")
                    .ExposePort(5432)
                    .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                    .ExportExploadedOnDispose(fullPath)
                    .Build()
                    .Start())
                {
                }

                Assert.IsTrue(Directory.Exists(fullPath));

                var files = Directory.GetFiles(fullPath).ToArray();
                Assert.IsTrue(files.Any(x => x.Contains("docker-entrypoint.sh")));
            }
            finally
            {
                if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, true);
            }
        }

        [TestMethod]
        public void ExportWithConditionDisposeShallWork()
        {
            var fullPath = (TemplateString)@"${TEMP}\fluentdockertest\${RND}\export.tar";
            // ReSharper disable once AssignNullToNotNullAttribute
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // Probably the oppsite is reverse where the last statement in the using clause
            // would set failure = false - but this is a unit test ;)
            var failure = false;
            try
            {
                // ReSharper disable once AccessToModifiedClosure
                using (new Builder().UseContainer()
                    .UseImage("kiasaki/alpine-postgres")
                    .ExposePort(5432)
                    .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                    .ExportOnDispose(fullPath, svc => failure)
                    .Build()
                    .Start())
                {
                    failure = true;
                }

                Assert.IsTrue(File.Exists(fullPath));
            }
            finally
            {
                if (File.Exists(fullPath))
                    Directory.Delete(Path.GetDirectoryName(fullPath), true);
            }
        }

        [TestMethod]
        public void CopyToRunningContainerShallWork()
        {
            var fullPath = (TemplateString)@"${TEMP}\fluentdockertest\${RND}\hello.html";

            // ReSharper disable once AssignNullToNotNullAttribute
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, "<html><head>Hello World</head><body><h1>Hello world</h1></body></html>");

            try
            {
                IList<Diff> before;
                using (
                    var container =
                        new Builder().UseContainer()
                            .UseImage("kiasaki/alpine-postgres")
                            .ExposePort(5432)
                            .WithEnvironment("POSTGRES_PASSWORD=mysecretpassword")
                            .Build()
                            .Start()
                            .WaitForProcess("postgres", 30000 /*30s*/)
                            .Diff(out before)
                            .CopyTo("/bin", fullPath))
                {
                    var after = container.Diff();

                    Assert.IsFalse(before.Any(x => x.Item == "/bin/hello.html"));
                    Assert.IsTrue(after.Any(x => x.Item == "/bin/hello.html"));
                }
            }
            finally
            {
                if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, true);
            }
        }
    }
}