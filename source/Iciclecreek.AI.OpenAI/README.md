# Iciclecreek.AI.OpenAI
This library defines Recognizer classes which use OpenAI to recognize multiple intents in the text of a user as function calls.

## Details
This library thinks about the problem of recognizing multiple intents in the 
text of a user as identifying the **set of functions** the user has requested, 
not as identifying functions to chain together.

This library provides a class **FunctionsRecognizer** which allows you to simply define the function signatures that should
be identified in the content of the user text.

It will return the set of functions that the user has requested in structured form. 


## Example
This class defines a MathFunctionRecognizer that recognizes the natural language of a user to request a math function.
```c#
    internal class MathFunctionRecognizer : FunctionsRecognizer
    {
        public MathFunctionRecognizer(OpenAIClient client) : base(client)
        {
            this.Functions.Add(new FunctionSignature("Add(x,y)", "Add two numbers", "what is 5 + 3? => Add(5,3)"));
            this.Functions.Add(new FunctionSignature("Subtract(x,y)", "Subtract two numbers", "remove 10 from 20 => Subtract(20,10)"));
            this.Functions.Add(new FunctionSignature("Multiply(fact1,fact2)", "Multiply two numbers", "32x16 => Multiply(32,16)"));
            this.Functions.Add(new FunctionSignature("Divide(num,denom)", "Divide two numbers", "divide 100 x 4 => Divide(100,4)"));
        }
    }
```

If you provide the text "what is 5 + 3?" to the MathFunctionRecognizer, it will return the following:
```c#
	{
		"Add": {
			"x": 5,
			"y": 3
		}
	}
```

More importantly, if you provide the text "what is 5 + 3? remove 10 from 20", it will return the following:
```c#
	{
		"Add": {
			"x": 5,
			"y": 3
		},
		"Subtract": {
			"x": 20,
			"y": 10
		}
	}
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
