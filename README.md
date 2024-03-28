# Iciclecreek.AI.OpenAI
This library defines Recognizer classes which use OpenAI to recognize multiple intents in the text of a user as function calls.

## Details
This library thinks about the problem of recognizing multiple intents in the text of a user as identifying the **set of functions** the user has requested, 
not as identifying functions to chain together.

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


# Iciclecreek.AI.OpenAI.FormFill
This library gives you a natural language form filling engine using **OpenAI**

## Details
This library defines the **FormFillEngine** class which uses **OpenAI** to recognize user intent in terms of function calls that are modifications to the properties on an object. *(aka natural language form fill)*

## Example
Given a model like this:
```C#
public class SampleModel
{
	public string? Name { get; set; }
	public int? Age { get; set; }
	public string? City { get; set; }
	public StatesEnum? State { get; set; }
}
```

Define an instance of the **FormFillEngine** using DependencyInjection like this:
```C#
    var services = new ServiceCollection();
	    // define the OpenAI client
		.AddSingleton<OpenAIClient>((sp) => new OpenAIClient(sp.GetRequiredService<IConfiguration>()["OpenAIKey"]));
		// add the form fill engine for SampleModel
		.AddFormFillEngine<SampleModel>();
		// ...
		.BuildServiceProvider();
```

Then use that engine to interpret user text into changes to the model.
```c#

	var formFillEngine = services.GetService<FormFillEngine<SampleModel>>();

	var model = new SampleModel();

	await formFillEngine.InterpretTextAsync("I am Fred, and I'm 42 years old", model);
	// model.Name = "Fred"
	// model.Age = 42
```

