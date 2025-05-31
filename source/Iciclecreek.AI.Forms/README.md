# Iciclecreek.AI.OpenAI.FormFill
This library gives you a natural language form filling engine using **OpenAI****

## Details
This library defines the **FormFillEngine** class which uses OpenAI to recognize 
user intent in terms of function calls that are modifications to the properties on an object. 
*(aka natural language form fill)*

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

You define an instance of the **FormFillEngine** like this:
```C#
    var services = new ServiceCollection();
	    // define the OpenAI client
		.AddSingleton<OpenAIClient>((sp) => new OpenAIClient(sp.GetRequiredService<IConfiguration>()["OpenAIKey"]));
		// add the form fill engine for SampleModel
		.AddFormFillEngine<SampleModel>();
		.BuildServiceProvider();
```

Then to use that engine
```c#

	var formFillEngine = services.GetService<FormFillEngine<SampleModel>>();

	var model = new SampleModel();

	await formFillEngine.InterpretTextAsync("I am Fred, and I'm 42 years old", model);
	// model.Name = "Fred"
	// model.Age = 42

	await formFillEngine.InterpretTextAsync("My name is Frank and I live in Iowa.", model);
	// model.Name = "Frank"
	// model.Age = 42
	// model.City = "Iowa"
```

The call to **InterpretTextAsync()** will parse the text using **OpenAI** and set the properties on the model as requested by the user. 

> NOTE: It returns a data structure which describes the operations that were performed.


