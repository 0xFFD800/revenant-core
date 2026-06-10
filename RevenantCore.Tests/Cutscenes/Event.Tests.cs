using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;

namespace RevenantCore.Tests.Cutscenes;

file static class EventFakes
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
public class EventFilter_Test
{
    [TestCase(false, TestName = "Evaluate HasNone (incomplete)")]
    [TestCase(true, TestName = "Evaluate HasNone (complete)")]
    public void HasNone_Evaluate(bool isComplete)
    {
        EventCollection collection = EventFakes.TestCollection;
        collection.Undo("NONE");
        if (isComplete)
            collection.Complete("NONE");
        EventFilter filter = new(new()
        {
            HasNone = ["NONE"]
        });
        Assert.AreNotEqual(isComplete, filter.Evaluate(collection));
    }

    [TestCase(false, TestName = "Evaluate HasAny (incomplete)")]
    [TestCase(true, TestName = "Evaluate HasAny (complete)")]
    public void HasAny_Evaluate(bool isComplete)
    {
        EventCollection collection = EventFakes.TestCollection;
        if (isComplete)
            collection.Complete("ANY");
        EventFilter filter = new(new()
        {
            HasAny = ["NOTREQ", "ANY"]
        });
        Assert.AreEqual(isComplete, filter.Evaluate(collection));
    }

    [TestCase(false, TestName = "Evaluate HasAll (incomplete)")]
    [TestCase(true, TestName = "Evaluate HasAll (complete)")]
    public void HasAll_Evaluate(bool isComplete)
    {
        EventCollection collection = EventFakes.TestCollection;
        collection.Complete("ALL");
        if (isComplete)
            collection.Complete("ALSO");
        EventFilter filter = new(new()
        {
            HasAll = ["ALL", "ALSO"]
        });
        Assert.AreEqual(isComplete, filter.Evaluate(collection));
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
        EventCollection collection = EventFakes.TestCollection;
        collection.Undo("NONE");
        Assert.IsFalse(collection.IsComplete("NONE"));
    }

    [Test]
    public void Complete_IsComplete_True()
    {
        EventCollection collection = EventFakes.TestCollection;
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
            new ErrorPrecondition(EventFakes.TestEvent)
                .Evaluate(EventFakes.TestCollection));
    }

    [Test]
    public void IgnorePrecondition_Bypass_False()
    {
        Assert.IsFalse(new IgnorePrecondition(EventFakes.TestEvent)
            .Evaluate(EventFakes.TestCollection));
    }

    [Test]
    public void ForcePrecondition_Bypass_CompletePreconditions()
    {
        EventCollection collection = EventFakes.TestCollection;
        EventSpec evt = EventFakes.TestEvent;
        Assert.IsTrue(new ForcePrecondition(evt).Evaluate(collection), "Evaluation result did not match expectation");
        Assert.IsFalse(collection.IsComplete("NONE"));
        Assert.IsTrue(collection.IsComplete("ANY"));
        Assert.IsFalse(collection.IsComplete("NOTREQ"));
        Assert.IsTrue(collection.IsComplete("ALL"));
        Assert.IsTrue(collection.IsComplete("ALSO"));
    }
}