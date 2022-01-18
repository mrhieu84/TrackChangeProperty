using System;
using System.Collections.Generic;
using System.IO;
using AssemblyToProcess;
using Fody;
using Framework;
using Xunit;

#pragma warning disable 618
#region WeaverTests
public class WeaverTests
{
    static Fody.TestResult testResult;

    static WeaverTests()
    {
        var weavingTask = new ModuleWeaver();
    
        testResult = weavingTask.ExecuteTestRun(@"AssemblyToProcess.dll", runPeVerify: false);
    }

    [Fact]
    public void ValidateIsInjected()
    {
        var instance = testResult.GetInstance("AssemblyToProcess.Class2");

        var countChanges = 0;
        instance.PropertyChanged   += new Action<string>( propname =>
        {
            countChanges++;
        });

        instance.PropBase = "abc";
        instance.Prop1 = "abc";
        instance.Prop2 = "abc";

        TrackDictionary<string, bool> changes = instance.ModifiedProperties;

        Assert.True(changes.ContainsKey("Prop1") && changes.ContainsKey("Prop2") && changes.ContainsKey("PropBase") && countChanges==3);
      //  Assert.True(changes.ContainsKey("IntVal2"));
    }

}
#endregion