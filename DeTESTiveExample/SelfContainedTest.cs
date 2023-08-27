using System.Collections.Generic;
using DeTESTive;
using FluentAssertions;
using UnityEngine;
using UniverseLib;

namespace DeTESTiveExample
{
    internal static class SelfContainedTest
    {
        private static List<string> s_assertionMessages = new();

        internal static DeTest Test =>
            CreateDeTest
                .WithDescription("Self-contained test that fails")
                .WithoutChangingTimescale()
                .WhichLoadsSaveFileByPath(MyPluginInfo.PLUGIN_NAME, nameWithExt: "TestSave.sodb")
                .WithSetup(Setup)
                .WhichAsserts(Assertions)
                .NoFailureHint()
                .WithTeardown(Teardown);

        private static System.Collections.IEnumerator Setup()
        {
            yield return RuntimeHelper.StartCoroutine(TestHelpers.DefaultSetup());

            // This next part shows how to get around assertion limitations, so
            // you can make multiple assertions with delays even though the
            // WhichAsserts assertion method needs to be synchronous. We pass
            // the information to the assertion method via class vars.

            // Do async setup for first assertion outside the try-catch block
            yield return new WaitForSeconds(1.0f);
            try
            {
                (1 + 1).Should().Be(2);
            }
            catch (System.Exception e)
            {
                // If you use FluentAssertions, you could specifically catch
                // FluentAssertions.Execution.AssertionFailedException instead
                // of System.Exception, but there are tradeoffs to either
                // approach.
                s_assertionMessages.Add(e.Message);
            }

            // Do async setup for second assertion outside the try-catch block
            yield return new WaitForSeconds(1.0f);
            try
            {
                (1 + 1).Should().Be(3);
            }
            catch (System.Exception e)
            {
                s_assertionMessages.Add(e.Message);
            }
        }

        private static bool Assertions()
        {
            // Continued from Setup, this will fail:
            s_assertionMessages.Should().BeEmpty();

            // You'd return false if you wanted to force a failure without
            // changing your assertions, but note that this line won't be read
            // in this case since the assertion error will occur on the line
            // above.
            return true;
        }

        private static System.Collections.IEnumerator Teardown()
        {
            yield return RuntimeHelper.StartCoroutine(TestHelpers.DefaultTeardown());
            // ...
        }
    }
}
