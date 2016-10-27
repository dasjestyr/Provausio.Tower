using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Castle.Windsor.Installer;

namespace Provausio.Tower.Api
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            InitializeComponents();
        }

        private static void InitializeComponents()
        {
            var container = new WindsorContainer();
            container.Install(FromAssembly.This());
            AppContainer.Container = container;

            var controllerActivator = new WindsorControllerActivator(AppContainer.Container);
            GlobalConfiguration.Configuration.Services.Replace(typeof(IHttpControllerActivator), controllerActivator);
        }
    }

    public class DisposeAction : IDisposable
    {
        private readonly Action _disposeAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="IDisposable"/> class.
        /// </summary>
        /// <param name="disposeAction">The dispose action.</param>
        public DisposeAction(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _disposeAction();
        }
    }

    public class ControllerInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // this allows for controllers living in different namespaces
            container.Register(Classes
                .FromThisAssembly()
                .Pick().If(t => t.Name.EndsWith("Controller"))
                .Configure(config => config.Named(config.Implementation.Name))
                .LifestylePerWebRequest());
        }
    }

    public class WindsorControllerActivator : IHttpControllerActivator
    {
        public static IWindsorContainer Container { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindsorControllerActivator"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public WindsorControllerActivator(IWindsorContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Creates the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="controllerDescriptor">The controller descriptor.</param>
        /// <param name="controllerType">Type of the controller.</param>
        /// <returns></returns>
        public IHttpController Create(
            HttpRequestMessage request,
            HttpControllerDescriptor controllerDescriptor,
            Type controllerType)
        {
            var controller = (IHttpController)Container.Resolve(controllerType);

            request.RegisterForDispose(new DisposeAction(() => Container.Release(controller)));

            return controller;
        }
    }
}
