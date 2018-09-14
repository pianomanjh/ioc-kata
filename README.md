# Dependency Injection Container - Code Kata

This exercise is a [Test Driven](https://en.wikipedia.org/wiki/Test-driven_development) [Code Kata](http://codekata.com/).  You will be building a [Dependency Injection Container](https://martinfowler.com/articles/injection.html), for registering and resolving class instances.
There are several cumulative enhancements you will make to your container.

### Setup
Please create a repository in github that consists of a both a library project, and a test project.  Then share that repository URL with us at Milliman.
Create a class `MyContainer` in your library project, and ensure you can build both projects.  
Make your first commit at this stage and push.

### Exercise
Work through each of the enhancements to your DI Container below, creating a **single commit**, with the new feature, and any tests covering the feature.
This exercise is intended to be test driven.  Therefore approach the problem step by step, each time writing a failing test, adding implementation to pass the test, and then refactoring if necessary.
After each commit, please push your commit to your github repository, and refrain from rebasing.  If you need to make a fix, feel free to make additional fixup commits.

This exercise should take 2-3 hours after this point.  Out of respect of your time, please do not spend more than 3 hours on this exercise.

### DI Container kata
-	A. Add the ability to Register a type. Ensure each call to Resolve return a new instance of that type
-	B. Add the ability to Register a type as a singleton.  Ensure each call to Resolve returns the same instance of that type
-	C. Add Constructor Injection.  The ability to Resolve a registered type using it's constructor that has one parameter of a registered type
-	D. Ensure that one can Register types in any order to get valid Resolve (this may mean simply adding more tests to show the existing design supports this requirement)
-	E. *(if time)* For constructor injection, add the ability to resolve a type using the 'greediest' constructor (one with most registered types)
-	F. *(if time)* Ensure a circular dependency throws an exception (instead of stack overflow)
- 	G. *(if time)* Ensure your container is thread-safe (resolve from different threads at once, and tests to show thread safety)
  
### Example
Below is a couple of tests, in C#, one might start with for item #1: 
``` csharp
    public interface IFoo { } 

    public class Foo : IFoo {} 

    [Test] 
    public void when_registering_a_type_should_return_an_instance_of_that_type() 
    { 
        var ioc = new MyContainer);
        ioc.Register<IFoo, Foo>(); 

        var instance1 = ioc.Resolve<IFoo>();

        Assert.IsTrue(instance1 is IFoo); 
    } 

    [Test] 
    public void when_registered_a_type_resolve_should_return_a_new_instance_of_that_type()
    { 
        var ioc = new MyContainer();
        ioc.Register<IFoo, Foo>(); 

        var instance1 = ioc.Resolve<IFoo>(); 
        var instance2 = ioc.Resolve<IFoo>();   

        Assert.IsTrue(instance1 != instance2); 
    } 
```


