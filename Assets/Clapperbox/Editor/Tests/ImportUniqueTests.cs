using System.Collections.Generic;

using DefaultNamespace;

using NUnit.Framework;


public class ScreenplayTests
{

    [Test]
    public void Single_Entry()
    {

        List<string> result = CharacterObjectsImport.GetUniqueSpeakers(
        new List<string>
        {
        "June",
        }
        );

        List<string> expected = new List<string>
        {
        "June",
        };

        Assert.That(result,
        Is.EqualTo(expected),
        $"- Expected [{string.Join(", ", expected)}] \n " +
        $"- Received [{string.Join(", ", result)}]");

    }

    [Test]
    public void Single_Entry_Mulipilied()
    {

        List<string> result = CharacterObjectsImport.GetUniqueSpeakers(
        new List<string>
        {
        "June",
        "June",
        "June",
        "June",
        "June",
        "June"
        }
        );

        List<string> expected = new List<string>
        {
        "June",
        };

        Assert.That(result,
        Is.EqualTo(expected),
        $"- Expected [{string.Join(", ", expected)}] \n " +
        $"- Received [{string.Join(", ", result)}]");

    }

    public void Multiple_Uniques()
    {

        List<string> result = CharacterObjectsImport.GetUniqueSpeakers(
        new List<string>
        {
        "June",
        "Marco",
        "Adrian",
        "Sandra",
        "Bob",
        "Mario"
        }
        );

        List<string> expected = new List<string>
        {
        "June",
        "Marco",
        "Adrian",
        "Sandra",
        "Bob",
        "Mario"
        };

        Assert.That(result,
        Is.EqualTo(expected),
        $"- Expected [{string.Join(", ", expected)}] \n " +
        $"- Received [{string.Join(", ", result)}]");

    }

    [Test]
    public void Multiple_Duplicates()
    {


        List<string> result = CharacterObjectsImport.GetUniqueSpeakers(
            new List<string>
        {
           "June",
           "Bob",
           "Sandra",
           "June",
           "Sandra",
           "Marcella",
           "Bob",
        }
        );

        List<string> expected = new List<string>
    {
        "June",
        "Sandra",
        "Marcella",
        "Bob",

    };

        Assert.That(
    result,
    Is.EqualTo(expected),
    $"- Expected [{string.Join(", ", expected)}] \n " +
    $"- Received [{string.Join(", ", result)}]");

    }



    [Test]
    public void Duplicate_indexed_last()
    {


        List<string> result = CharacterObjectsImport.GetUniqueSpeakers(
            new List<string>
        {
           "June",
           "Bob",
           "Sandra",
           "Marcella",
           "Bob",
        }
        );

        List<string> expected = new List<string>
    {
        "June",
        "Sandra",
        "Marcella",
        "Bob",

    };

        Assert.That(
    result,
    Is.EqualTo(expected),
    $"- Expected [{string.Join(", ", expected)}] \n " +
    $"- Received [{string.Join(", ", result)}]");

    }


}
