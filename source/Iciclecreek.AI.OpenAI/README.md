# Iciclecreek.AI.OpenAI
This library defines Recognizer classes which use OpenAI to recognize multiple intents in the text of a user as function calls.

## Details
This library thinks about the problem of recognizing multiple intents in the text of a user as identifying the **set of functions** the user has requested, not as identifying functions to chain together.

This library provides a class **SemanticActionRecognizer** which allows you to simply define the function signatures that should be identified in the content of the user text.

It will return the set of functions that the user has requested in structured form. 


## Example
This example class defines a MathFunctionRecognizer that recognizes the natural language of a user to request a math function.
```c#
internal class MathFunctionRecognizer : SemanticActionRecognizer
{
    public MathFunctionRecognizer(OpenAIClient client) : base(client)
    {
        this.Actions.Add(new SemanticActionDefinition("Add", "Add two numbers")
            .AddArgument("number").AddArgument("number")
            .AddExample("what is 5 + 3?", "5", "3"));
        this.Actions.Add(new SemanticActionDefinition("Subtract", "Subtract two numbers")
            .AddArgument("number").AddArgument("number")
            .AddExample("remove 10 from 20", "20", "10"));
        this.Actions.Add(new SemanticActionDefinition("Multiply", "Multiply two numbers")
            .AddArgument("number").AddArgument("number")
            .AddExample("32x16", "32", "16"));
        this.Actions.Add(new SemanticActionDefinition("Divide", "Divde two numbers")
            .AddArgument("number").AddArgument("number")
            .AddExample("divide 100 x 4", "100", "4"));
    }
}
```

If you provide the text "what is 5 + 3?" to the MathFunctionRecognizer, it will return the following:
```c#
Add('5', '3')
```

More importantly, if you provide the text "what is 5 + 3? remove 10 from 20", it will return multiple function signatures:
```c#
Add('5', '3')
Subtract('20', '10')
```

## NOTES
The arguments passed to the functions may be in the form of strings and require further parsing to get to value types you need. For
example, for the Add() function you may need to convert the x and y values to integers:

```c#
  void Add(object x, object y)
  {
	var xValue = Convert.ToInt32(x);
	var yValue = Convert.ToInt32(y);
	Console.WriteLine(xValue + yValue);
  })
```
