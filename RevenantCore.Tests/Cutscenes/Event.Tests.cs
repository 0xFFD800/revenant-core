using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;

namespace RevenantCore.Tests.Cutscenes;

file static class EventCollectionFakes
{    
    public static EventSpec TestEvent => new()
    {
        ID = "TEST",
        Preconditions = new()
        {
            HasNone = ["NONE"],
            HasAny = ["ANY", "NOTREQ"],
            HasAll = ["ALL", "ALSO"]
        }
    };

    public static EventCollection TestCollection
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
}

[TestFixture]
public class EventCollection_Test
{
    [Test]
    public void Initial_IsComplete_False()
    {
        Assert.IsFalse(new EventCollection([]).IsComplete("NOTCOMPLETE"));
    }

    [Test]
    public void Undo_IsComplete_False()
    {
        EventCollection collection = EventCollectionFakes.TestCollection;
        collection.Undo("NONE");
        Assert.IsFalse(collection.IsComplete("NONE"));
    }

    [Test]
    public void Complete_IsComplete_True()
    {
        EventCollection collection = EventCollectionFakes.TestCollection;
        collection.Complete("NOTREQ");
        Assert.IsTrue(collection.IsComplete("NOTREQ"));
    }
}

[TestFixture]
public class Precondition_Test
{
    [Test]
    public void ErrorPrecondition_Bypass_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => 
            new ErrorPrecondition(EventCollectionFakes.TestEvent)
                .Evaluate(EventCollectionFakes.TestCollection));
    }

    [Test]
    public void IgnorePrecondition_Bypass_False()
    {
        Assert.IsFalse(new IgnorePrecondition(EventCollectionFakes.TestEvent)
            .Evaluate(EventCollectionFakes.TestCollection));
    }

    [Test]
    public void ForcePrecondition_Bypass_CompletePreconditions()
    {
        EventCollection collection = EventCollectionFakes.TestCollection;
        EventSpec evt = EventCollectionFakes.TestEvent;
        Assert.IsTrue(new ForcePrecondition(evt).Evaluate(collection), "Evaluation result did not match expectation");
        Assert.IsFalse(collection.IsComplete("NONE"));
        Assert.IsTrue(collection.IsComplete("ANY"));
        Assert.IsFalse(collection.IsComplete("NOTREQ"));
        Assert.IsTrue(collection.IsComplete("ALL"));
        Assert.IsTrue(collection.IsComplete("ALSO"));
    }
}