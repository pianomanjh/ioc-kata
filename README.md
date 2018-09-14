# Dependency Injection Container - Code Kata

This exercise is a [Test Driven](https://en.wikipedia.org/wiki/Test-driven_development) [Code Kata](http://codekata.com/).  You will be building a [Dependency Injection Container](https://martinfowler.com/articles/injection.html), for registering and resolving class instances.

### Setup
Please create a repository in github that consists of a both a library project, and a test project.  Then share that repository URL with us at Milliman.
Create a class `MyContainer` in your library project, and ensure you can build both projects.  
Make your first commit at this stage and push.

### Exercise
Work through each of the enhancements to your DI Container below, creating a **single commit**, with the new feature, and any tests covering the feature.
After each commit, please push your commit to your github repository, and refrain from rebasing.  If you need to make a fix, feel free to make any additional fixup commits.

This exercise should take 2-3 hours after this point.  Out of respect of your time, please do not spend more than 3 hours on this exercise.

### DI Container kata
-	A. Add the ability to Register a class instance, and ensure that instance is returned on a call to Resolve
-	B. Add the ability to Register a type. Ensure each call to Resolve return a new instance of that type
-	C. Add the ability to Register a type as a singleton.  Ensure each call to Resolve returns the same instance of that type
-	D. Add Constructor Injection.  The ability to Resolve a registered type using it's constructor that has one parameter of a registered type
-	E. Ensure that one can Register types in any order to get valid Resolve (this may mean simply adding more tests to show the existing design supports this requirement)
-	F. *(if time)* For constructor injection, add the ability to resolve a type using the 'greediest' constructor (one with most registered types)
-	G. *(if time)* Ensure a circular dependency throws an exception (instead of stack overflow)
- H. *(if time)* Ensure your container is thread-safe (resolve from different threads at once, and tests to show thread safety)
  
### Example
To test the first requirement, A, I may create a test like the following:
```csharp
	[Test]
	public void Register_Resolve_Instance()
	{		
		var ioc = new MyDI();
		
		var foo = new Foo();
		ioc.Register<IFoo, Foo>(foo);
			
		var instance = ioc.Resolve<IFoo>();
	
		Assert.IsTrue(instance == foo);
	}
 ```


