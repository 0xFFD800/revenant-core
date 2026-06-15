using RevenantCore.Util;

namespace RevenantCore.Tests.Util;

public enum TestEnum { AllNull, Blank, Full }

public class TestObject
{
    public bool? Bool { get; set; }
    public float? Number { get; set; }
    public string? String { get; set; }
}


[TestFixture]
public class Spec_Test
{
    private static readonly Dictionary<TestEnum, TestObject> InMemoryTest = new()
    {
        { TestEnum.AllNull, new() { Bool = null, Number = null, String = null } },
        { TestEnum.Blank, new() { Bool = false, Number = 0, String = "" } },
        { TestEnum.Full, new() { Bool = true, Number = 3.1415927F, String = "Lorem ipsum dolor amet" } }
    };

    private static readonly string YamlDocumentTest = """
    allNull:
      bool: 
      number: 
      string: 
    blank:
      bool: false
      number: 0
      string: ''
    full:
      bool: true
      number: 3.1415927
      string: Lorem ipsum dolor amet
    
    """;

    [Test]
    public void SerializerMatches()
    {
        Assert.AreEqual(YamlDocumentTest, Serializers.CreateSerializer([]).Serialize(InMemoryTest));
    }

    [Test]
    public void DeserializerMatches()
    {
        Dictionary<TestEnum, TestObject> deserialized = Serializers.CreateDeserializer([]).Deserialize<Dictionary<TestEnum, TestObject>>(YamlDocumentTest);
        foreach (TestEnum test in Enum.GetValues<TestEnum>())
        {
            Assert.AreEqual(InMemoryTest[test].Bool, deserialized[test].Bool,
                Enum.GetName(test) + "'s Bool value did not match expectation");
            Assert.AreEqual(InMemoryTest[test].Number, deserialized[test].Number,
                Enum.GetName(test) + "'s Number value did not match expectation");
            Assert.AreEqual(InMemoryTest[test].String, deserialized[test].String,
                Enum.GetName(test) + "'s String value did not match expectation");
        }
    }
}