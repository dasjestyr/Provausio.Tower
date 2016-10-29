using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Provausio.Tower.Api.Data;
using Provausio.Tower.Core;

namespace Provausio.Tower.Api.App_Start.IoC
{
    public class ServiceInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component
                .For<IPubSubHub>()
                .ImplementedBy<Hub>()
                .LifestyleSingleton());

            container.Register(Component
                .For<IChallengeGenerator>()
                .ImplementedBy<Sha256ChallengeGenerator>()
                .LifestyleSingleton());

            container.Register(Component
                .For<ISubscriptionStore>()
                .ImplementedBy<InMemorySubscriptionStore>()
                .LifestyleSingleton());
        }
    }
}
