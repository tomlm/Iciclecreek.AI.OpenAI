# Iciclecreek.AI.OpenAI.FormFill
This library gives you a natural language form filling engine using OpenAI.

## Details
This defines the **FormFillEngine** class which uses OpenAI to recognize 
user intent in terms of modifications to the properties on an object. (aka natural language form fill)

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

You define a instance of the **FormFillEngine** like this:
```C#
    var services = new ServiceCollection();
		.AddSingleton<OpenAIClient>((sp) => new OpenAIClient(sp.GetRequiredService<IConfiguration>()["OpenAIKey"]));
		.AddFormFillEngine<SampleModel>();
		.BuildServiceProvider();
```

Then to use that engine
```c#

	var model = new SampleModel();
	var formFillEngine = services.GetService<FormFillEngine<SampleModel>>();

	await formFillEngine.InterpretTextAsync("I am Fred, and I'm 42 years old", model);
	// model.Name = "Fred"
	// model.Age = 42

	await formFillEngine.InterpretTextAsync("My name is Frank and I live in Iowa.", model);
	// model.Name = "Frank"
	// model.Age = 42
	// model.City = "Iowa"
```

The call to InterpretTextAsync will parse the text and set the properties on the model as requested by the user. It returns
a data structure which describes the operations that were performed.


