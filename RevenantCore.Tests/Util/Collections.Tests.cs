using RevenantCore.Util;

namespace RevenantCore.Tests.Util;

[TestFixture]
public class OrderedDict_Test
{
    [Test(Description = "Adding a new key should create a new value list")]
    public void Add_NewKey_CreateList()
    {
        OrderedDict<int, int> dict = new();

        bool result = dict.Add(0, 0);

        Assert.True(result, "Adding a new key should return true");
        Assert.AreEqual(1, dict.Get(0).Length, "A new key should be reflected in the output of Get");
    }

    [Test(Description = "Adding a new value to an existing key should update the value list")]
    public void Add_ExtKeyNewValue_UpdateList()
    {
        const int NEW_VAL = 1;

        OrderedDict<int, int> dict = new();
        dict.Add(0, 0);
        
        bool result = dict.Add(0, NEW_VAL);
        int[] values = dict.Get(0);

        Assert.True(result, "Adding a new value to an existing key should return true");
        Assert.AreEqual(2, values.Length, "Adding a new value to an existing key should update the value list");
        Assert.AreEqual(NEW_VAL, values[1], "New values for existing keys should appear at the end of the value list");
    }

    [Test(Description = "Adding an existing value to an existing key should be ignored")]
    public void Add_ExtValue_Ignore()
    {
        OrderedDict<int, int> dict = new();
        dict.Add(0, 0);
        
        bool result = dict.Add(0, 0);

        Assert.False(result, "Adding an existing value to an existing key should return false");
        Assert.AreEqual(1, dict.Get(0).Length, "Duplicate values should not be added to the value list");
    }

    [Test(Description = "Get should return an empty array if key is not in the dictionary")]
    public void Get_MissingKey_Empty()
    {
        OrderedDict<int, int> dict = new();

        int[] result = dict.Get(0);

        Assert.NotNull(result, "Get should never return null");
        Assert.AreEqual(0, result.Length, "Get should return an empty array if the key is missing");
    }

    [Test(Description = "If remove is called on a missing key, it should do nothing and return false")]
    public void Remove_MissingKey_False()
    {
        OrderedDict<int, int> dict = new();
        dict.Add(1, 1);

        bool result = dict.Remove(0, 0);

        Assert.False(result, "Remove should return false for a missing key");
        Assert.True(dict.Has(1), "Remove should not affect unrelated keys");
    }

    [Test(Description = "If remove is called on a present key but missing value, it should do nothing and return false")]
    public void Remove_MissingVal_False()
    {
        OrderedDict<int, int> dict = new();
        dict.Add(0, 1);

        bool result = dict.Remove(0, 0);

        Assert.False(result, "Remove should return false for a missing value");
        Assert.AreEqual(1, dict.Get(0).Length, "Remove should not affect unrelated keys");
    }

    [Test(Description = "If remove is called on a present key/value pair, it should remove only that pair and return true")]
    public void Remove_Present_True()
    {
        OrderedDict<int, int> dict = new();
        dict.Add(1, 0);
        dict.Add(0, 0);
        dict.Add(0, 1);

        bool result = dict.Remove(0, 0);

        Assert.True(result, "Remove should return true for a present value");
        Assert.AreEqual(1, dict.Get(0).Length, "Remove should not affect unrelated keys or values");
        Assert.AreEqual(1, dict.Get(1).Length, "Remove should not affect unrelated keys or values");
    }

    [Test(Description = "If the last value for a key is removed, the key should also be removed")]
    public void Remove_LastVal_RemoveKey()
    {
        OrderedDict<int, int> dict = new();
        dict.Add(0, 0);

        bool result = dict.Remove(0, 0);

        Assert.True(result, "Remove should return true for a present value");
        Assert.False(dict.Has(0), "Remove should get rid of keys which no longer have values");
    }
    
    [Test(Description = "Sort should apply the comparator to all value lists")]
    public void Sort_AllKeys()
    {
        OrderedDict<int, int> dict = new();
        dict.Add(0, 2);
        dict.Add(0, 1);
        dict.Add(1, 2);
        dict.Add(1, 3);
        dict.Add(1, 1);

        dict.Sort(Comparer<int>.Create((x, y) => x - y));

        int[] expValues0 = [1, 2];
        int[] expValues1 = [1, 2, 3];
        int[] values0 = dict.Get(0);
        int[] values1 = dict.Get(1);
        Assert.AreEqual(expValues0, values0, "Sorted values for 0 did not match expectation");
        Assert.AreEqual(expValues1, values1, "Sorted values for 1 did not match expectation");
    }
}