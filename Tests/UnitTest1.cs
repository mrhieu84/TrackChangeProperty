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


        instance.Prop2 = 1; // countTrigger++;
        instance.lst2[0].Text = "abc"; //countTrigger++
        instance.lst3.Add("123"); //countTrigger++

        TrackDictionary<string, bool> changes = instance.ModifiedProperties;

      
        changes.Clear(); 
        var cc = instance.lst2[0];
        cc.Text = "456"; //countTrigger++


        instance.lst2.Remove(cc); //countTrigger++;
        changes.Clear();
        cc.Text = "789";

        instance.lst3[0] = "123";
        instance.lst3.Remove(instance.lst3[0]); //countTrigger++

       // total countTrigger = 6
        Assert.True(countTrigger==6);
      
    }

    private void testEvent(object sender, PropertyChangedArgs e)
    {
        countTrigger++;
    }
}
#endregion