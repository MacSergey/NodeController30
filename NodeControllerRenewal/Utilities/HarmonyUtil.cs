namespace KianCommons
{
    using CitiesHarmony.API;
    using HarmonyLib;
    using System.Reflection;
    using System;
    using System.Runtime.CompilerServices;
    using static KianCommons.ReflectionHelpers;
    using NodeController30;

    public static class HarmonyUtil
    {
        static bool harmonyInstalled_ = false;
        const string errorMessage_ =
                    "****** ERRRROOORRRRRR!!!!!!!!!! **************\n" +
                    "**********************************************\n" +
                    "    HARMONY MOD DEPENDANCY IS NOT INSTALLED!\n\n" +
                    "solution:\n" +
                    " - exit to desktop.\n" +
                    " - unsub harmony mod.\n" +
                    " - make sure harmony mod is deleted from the content folder\n" +
                    " - resub to harmony mod.\n" +
                    " - run the game again.\n" +
                    "**********************************************\n" +
                    "**********************************************\n";

        internal static void AssertCitiesHarmonyInstalled()
        {
            if (!HarmonyHelper.IsHarmonyInstalled)
                throw new Exception(errorMessage_);
        }

        internal static void InstallHarmony(string harmonyID)
        {
            try
            {
                if (harmonyInstalled_)
                {
                    Mod.Logger.Debug("skipping harmony installation because its already installed");
                    return;
                }
                AssertCitiesHarmonyInstalled();
                Mod.Logger.Debug("Patching...");
                PatchAll(harmonyID);
                harmonyInstalled_ = true;
                Mod.Logger.Debug("Patched.");
            }
            catch (TypeLoadException ex)
            {
                Mod.Logger.Error(new TypeLoadException(errorMessage_, ex));
            }
            catch (Exception ex)
            {
                Mod.Logger.Error(ex);
            }
        }

        /// <typeparam name="T">Only install classes with this attribute</typeparam>
        internal static void InstallHarmony<T>(string harmonyID) where T : Attribute
        {
            try
            {
                AssertCitiesHarmonyInstalled();
                Mod.Logger.Debug("Patching...");
                PatchAll(harmonyID, required: typeof(T));
                Mod.Logger.Debug("Patched.");
            }
            catch (TypeLoadException ex)
            {
                Mod.Logger.Error(new TypeLoadException(errorMessage_, ex));
            }
            catch (Exception ex)
            {
                Mod.Logger.Error(ex);
            }
        }

        internal static void InstallHarmony(string harmonyID, Type required = null, Type forbidden = null)
        {
            try
            {
                AssertCitiesHarmonyInstalled();
                Mod.Logger.Debug("Patching...");
                PatchAll(harmonyID, required: required, forbidden: forbidden);
                Mod.Logger.Debug("Patched.");
            }
            catch (TypeLoadException ex)
            {
                Mod.Logger.Error(new TypeLoadException(errorMessage_, ex));
            }
            catch (Exception ex)
            {
                Mod.Logger.Error(ex);
            }
        }


        /// <summary>
        /// assertion shall take place in a function that does not refrence Harmony.
        /// </summary>
        /// <param name="harmonyID"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void PatchAll(string harmonyID)
        {
            var harmony = new Harmony(harmonyID);
            harmony.PatchAll();
            harmony.LogPatchedMethods();
        }

        /// <summary>
        /// assertion shall take place in a function that does not refrence Harmony.
        /// Only install classes with this attribute
        /// </summary>
        /// <param name="harmonyID"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void PatchAll(string harmonyID, Type required = null, Type forbidden = null)
        {
            var harmony = new Harmony(harmonyID);
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
            {
                try
                {
                    if (required is not null && !type.HasAttribute(required))
                        continue;
                    if (forbidden is not null && type.HasAttribute(forbidden))
                        continue;
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception ex)
                {
                    Mod.Logger.Error(ex);
                }
            }
            harmony.LogPatchedMethods();
        }

        public static void LogPatchedMethods(this Harmony harmony)
        {
            foreach (var method in harmony.GetPatchedMethods())
                Mod.Logger.Debug($"harmony({harmony.Id}) patched: {method.DeclaringType.FullName}::{method.Name}");
        }

        internal static void UninstallHarmony(string harmonyID)
        {
            AssertCitiesHarmonyInstalled();
            Mod.Logger.Debug("UnPatching...");
            UnpatchAll(harmonyID);
            harmonyInstalled_ = false;
            Mod.Logger.Debug("UnPatched.");
        }

        /// <summary>
        /// assertion shall take place in a function that does not refrence Harmony.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void UnpatchAll(string harmonyID)
        {
            var harmony = new Harmony(harmonyID);
            harmony.UnpatchAll(harmonyID);
        }

        internal static void ManualPatch<T>(string harmonyID)
        {
            AssertCitiesHarmonyInstalled();
            ManualPatchUnSafe(typeof(T), harmonyID);
        }
        internal static void ManualPatch(Type t, string harmonyID)
        {
            AssertCitiesHarmonyInstalled();
            ManualPatchUnSafe(t, harmonyID);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ManualPatchUnSafe(Type t, string harmonyID)
        {
            try
            {
                MethodBase targetMethod =InvokeMethod(t, "TargetMethod") as MethodBase;
                Mod.Logger.Debug($"{t.FullName}.TorgetMethod()->{targetMethod}");
                Assertion.AssertNotNull(targetMethod, $"{t.FullName}.TargetMethod() returned null");
                var prefix = GetHarmonyMethod(t, "Prefix");
                var postfix = GetHarmonyMethod(t, "Postfix");
                var transpiler = GetHarmonyMethod(t, "Transpiler");
                var finalizer = GetHarmonyMethod(t, "Finalizer");
                var harmony = new Harmony(harmonyID);
                harmony.Patch(original: targetMethod, prefix: prefix, postfix: postfix, transpiler: transpiler, finalizer: finalizer);
            }
            catch (Exception e)
            {
                Mod.Logger.Error(e);
            }
        }

        public static HarmonyMethod GetHarmonyMethod(Type t, string name)
        {
            var m = GetMethod(t, name, throwOnError: false);
            if (m == null) 
                return null;
            Assertion.Assert(m.IsStatic, $"{m}.IsStatic");
            return new HarmonyMethod(m);
        }
    }
}