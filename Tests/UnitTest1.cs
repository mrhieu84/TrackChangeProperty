using System;
using System.Collections.Generic;
using System.IO;
using AssemblyToProcess;
using Fody;
using TrackChangePropertyLib;
using Xunit;

#pragma warning disable 618
#region WeaverTests
public class WeaverTests
{
    static Fody.TestResult testResult;

    static WeaverTests()
    {
        var weavingTask = new ModuleWeaver();
      
        testResult = weavingTask.ExecuteTestRun(@"AssemblyToProcess.dll", runPeVerify: false  );
    }

    private int countTrigger = 0;
    [Fact]
    public void ValidateIsInjected()
    {
        var instance = testResult.GetInstance("AssemblyToProcess.Class2");

        instance.PropertyChange += new EventHandler<PropertyChangedArgs>(testEvent);
        instance.Item.Name = "abc";


        TrackDictionary<string, bool> changes = instance.ModifiedProperties;

      
        changes.Clear(); 
       
       // total countTrigger = 6
        Assert.True(countTrigger==1);
      
    }

    private void testEvent(object sender, PropertyChangedArgs e)
    {
        countTrigger++;
    }
}
#endregion