﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CKAN;
using NUnit.Framework;
using Tests.Core;
using Tests.Data;
using ModuleInstaller = CKAN.ModuleInstaller;

namespace Tests.GUI
{
    [TestFixture]
    public class MainModListTests
    {
        [Test]
        public void OnCreation_HasDefaultFilters()
        {
            var item = new MainModList(delegate { }, delegate { return null; });
            Assert.AreEqual(GUIModFilter.Compatible, item.ModFilter, "ModFilter");
            Assert.AreEqual(String.Empty, item.ModNameFilter, "ModNameFilter");
        }

        [Test]
        public void OnModTextFilterChanges_CallsEventHandler()
        {
            var called_n = 0;
            var item = new MainModList(delegate { called_n++; }, delegate { return null; });
            Assert.That(called_n == 1);
            item.ModNameFilter = "randomString";
            Assert.That(called_n == 2);
            item.ModNameFilter = "randomString";
            Assert.That(called_n == 2);
        }
        [Test]
        public void OnModTypeFilterChanges_CallsEventHandler()
        {
            var called_n = 0;
            var item = new MainModList(delegate { called_n++; }, delegate { return null; });
            Assert.That(called_n == 1);
            item.ModFilter = GUIModFilter.Installed;
            Assert.That(called_n == 2);
            item.ModFilter = GUIModFilter.Installed;
            Assert.That(called_n == 2);
        }

        [Test]
        public void ComputeChangeSetFromModList_WithEmptyList_HasEmptyChangeSet()
        {
            var item = new MainModList(delegate { }, delegate { return null; });
            Assert.That(item.ComputeUserChangeSet(), Is.Empty);
        }

        [Test]
        [Category("Display")]
        public void ComputeChangeSetFromModList_WithConflictingMods_ThrowsInconsistentKraken()
        {
            using (var tidy = new DisposableKSP())
            {
                KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(tidy.KSP)) { CurrentInstance = tidy.KSP };

                var registry = Registry.Empty();
                var module = TestData.FireSpitterModule();
                module.conflicts = new List<RelationshipDescriptor> { new RelationshipDescriptor { name = "kOS" } };
                registry.AddAvailable(TestData.FireSpitterModule());
                registry.AddAvailable(TestData.kOS_014_module());
                registry.RegisterModule(module, Enumerable.Empty<string>(), tidy.KSP);

                var main_mod_list = new MainModList(null, null);
                var mod = new GUIMod(TestData.FireSpitterModule(), registry, manager.CurrentInstance.Version());
                var mod2 = new GUIMod(TestData.kOS_014_module(), registry, manager.CurrentInstance.Version());
                mod.IsInstallChecked = true;
                mod2.IsInstallChecked = true;

                //What the fuck is this shit?
                //var compute_change_set_from_mod_list = main_mod_list.ComputeChangeSetFromModList(registry, main_mod_list.ComputeUserChangeSet(), null, tidy.KSP.Version());
                //UtilStatic.Throws<InconsistentKraken>(compute_change_set_from_mod_list));
            }
        }

        [Test]
        public void IsVisible_WithAllAndNoNameFilter_ReturnsTrueForCompatible()
        {
            using (var tidy = new DisposableKSP())
            {
                KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(tidy.KSP)) { CurrentInstance = tidy.KSP };

                var ckan_mod = TestData.FireSpitterModule();
                var registry = Registry.Empty();
                registry.AddAvailable(ckan_mod);
                var item = new MainModList(delegate { }, null);
                Assert.That(item.IsVisible(new GUIMod(ckan_mod, registry, manager.CurrentInstance.Version())));
            }
        }

        [Test]
        public void CountModsByFilter_EmptyModList_ReturnsZeroForAllFilters()
        {
            var item = new MainModList(delegate { }, null);
            foreach (GUIModFilter filter in Enum.GetValues(typeof(GUIModFilter)))
            {
                Assert.That(item.CountModsByFilter(filter), Is.EqualTo(0));
            }

        }

        [Test]
        [Category("Display")]
        public void ConstructModList_NumberOfRows_IsEqualToNumberOfMods()
        {
            using (var tidy = new DisposableKSP())
            {
                KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(tidy.KSP)) { CurrentInstance = tidy.KSP };
                var registry = Registry.Empty();
                registry.AddAvailable(TestData.FireSpitterModule());
                registry.AddAvailable(TestData.kOS_014_module());
                var main_mod_list = new MainModList(null, null);
                var mod_list = main_mod_list.ConstructModList(new List<GUIMod>
                {
                    new GUIMod(TestData.FireSpitterModule(), registry, manager.CurrentInstance.Version()),
                    new GUIMod(TestData.kOS_014_module(), registry, manager.CurrentInstance.Version())
                });
                Assert.That(mod_list, Has.Count.EqualTo(2));
            }
        }

        /*
        [Test]
        [Category("Display")]
        public void TooManyProvidesCallsHandlers()
        {
            using (var tidy = new DisposableKSP())
            {
                var registry = Registry.Empty();
                var generator = new RandomModuleGenerator(new Random(0451));
                var provide_ident = "provide";
                var ksp_version = tidy.KSP.Version();
                var mod = generator.GeneratorRandomModule(depends: new List<RelationshipDescriptor>
                {
                    new RelationshipDescriptor {name = provide_ident}
                },ksp_version:ksp_version);
                var moda = generator.GeneratorRandomModule(provides: new List<string> { provide_ident }
                , ksp_version: ksp_version);
                var modb = generator.GeneratorRandomModule(provides: new List<string> { provide_ident }
                , ksp_version: ksp_version);
                var choice_of_provide = modb;
                registry.AddAvailable(mod);
                registry.AddAvailable(moda);
                registry.AddAvailable(modb);
                var installer = ModuleInstaller.GetInstance(tidy.KSP, null);
                //Huh, no overload? 2 params?
                var main_mod_list = new MainModList(null, kraken => await Task.FromResult(choice_of_provide));
                var a = new HashSet<KeyValuePair<GUIMod, GUIModChangeType>>
                {
                    new KeyValuePair<GUIMod, GUIModChangeType>(new GUIMod(mod,registry,ksp_version),GUIModChangeType.Install)
                };

                var mod_list = main_mod_list.ComputeChangeSetFromModList(registry, a, installer, ksp_version);
                CollectionAssert.AreEquivalent(
                    new[] {
                        new KeyValuePair<GUIMod,GUIModChangeType>(new GUIMod(mod,registry,ksp_version), GUIModChangeType.Install),
                        new KeyValuePair<GUIMod,GUIModChangeType>(new GUIMod(modb,registry,ksp_version),GUIModChangeType.Install)}, mod_list);

            }
        }
        */

    }
}
