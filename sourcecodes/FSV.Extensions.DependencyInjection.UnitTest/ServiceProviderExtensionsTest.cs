// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Schäfer, Matthias Friedrich, and Ritesh Gite
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace FSV.Extensions.DependencyInjection.UnitTest
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class ServiceProviderExtensionsTest
    {
        [Fact]
        public void ServiceProviderExtensions_GetRequiredService_resolves_component_with_internal_ctor_Test()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<IFoo, Foo>();
            services.AddTransient<ComponentWithInternalCtor>();


            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Act
            var resolvedParameters = new ResolverOverride[] { };
            var actual = serviceProvider.GetRequiredService<ComponentWithInternalCtor>(resolvedParameters);

            // Assert
            Assert.NotNull(actual);
        }
    }

    public class Foo : IFoo
    {
        public void Bar()
        {
        }
    }

    public class ComponentWithInternalCtor
    {
        private readonly IFoo foo;

        internal ComponentWithInternalCtor(IFoo foo)
        {
            this.foo = foo ?? throw new ArgumentNullException(nameof(foo));
        }

        private ComponentWithInternalCtor(IFoo foo, string s)
        {
            throw new InvalidOperationException("The private ctor must not be called by the service-provider.");
        }
    }

    internal interface IFoo
    {
        void Bar();
    }
}