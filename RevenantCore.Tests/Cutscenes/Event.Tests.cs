using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;

namespace RevenantCore.Tests.Cutscenes;

[TestFixture]
public class Precondition_Test
{
    private static EventSpec TestEvent => new()
    {
        ID = "TEST",
        Preconditions = new()
        {
            HasNone = ["NONE"],
            HasAny = ["ANY", "NOTREQ"],
            HasAll = ["ALL", "ALSO"]
        }
    };

    private static EventCollection TestCollection
    {
        get
        {
            EventCollection value = new([
                new()
                {
                    ID = "NONE"
                },
                new()
                {
                    ID = "ANY"
                },
                new()
                {
                    ID = "NOTREQ"
                },
                new()
                {
                    ID = "ALL"
                },
                new()
                {
                    ID = "ALSO"
                }
            ]);
            value.Complete("NONE");
            return value;
        }
    }

    [Test]
    public void ErrorPrecondition_Bypass_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => 
            new ErrorPrecondition(TestEvent).Evaluate(TestCollection));
    }

    [Test]
    public void IgnorePrecondition_Bypass_False()
    {
        Assert.IsFalse(new IgnorePrecondition(TestEvent).Evaluate(TestCollection));
    }

    [Test]
    public void ForcePrecondition_Bypass_CompletePreconditions()
    {
        EventCollection collection = TestCollection;
        EventSpec evt = TestEvent;
        Assert.IsTrue(new ForcePrecondition(evt).Evaluate(collection), "Evaluation result did not match expectation");
        Assert.IsFalse(collection.IsComplete("NONE"));
        Assert.IsTrue(collection.IsComplete("ANY"));
        Assert.IsFalse(collection.IsComplete("NOTREQ"));
        Assert.IsTrue(collection.IsComplete("ALL"));
        Assert.IsTrue(collection.IsComplete("ALSO"));
    }
}