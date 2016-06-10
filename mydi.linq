<Query Kind="Program" />

void Main()
{
	var tests = new MyIocTests();
	tests.RunTests();
	
	/* DI Container kata
	1. ensure a container only resolves types that were registered
	2. ability to register an instance, and ensure it resolves
	3. ability to register a type. Ensure it resolves
	4. ability to register a singleton.  Each resolve return same instance	
	5. DI: ability to resolve a type with a constructor of one parameter of a registered type
	6. DI: can register types in any order to get valid resolve
	7. DI: ability to resolve a type using the 'greediest' constructor (one with most registered types)
	8. DI: ensure a circular dependency throws an exception (instead of stack overflow)
	*/
}

public interface IFoo { }
public class Foo : IFoo
{
	public string Test { get; set; }
}

public interface IBar { IFoo Foo { get; } }
public class Bar : IBar
{
	public Bar()
	{	
	}
	
	public IFoo Foo { get; private set; }
	
	public Bar(IFoo foo)
	{
		Foo = foo;
	}
}

public class Zoo
{
	public IFoo Foo { get; private set; }
	public IBar Bar { get; private set; }

	public Zoo() {}
	
	public Zoo(IFoo foo)
	{
		Foo = foo;
	}
	
	public Zoo(IFoo foo, IBar bar)
	{
		Foo = foo;
		Bar = bar;
	}
}

public class CircleDepA
{
	public CircleDepB B { get; private set;}

	public CircleDepA() {}

	public CircleDepA(CircleDepB b)
	{
		B = b;
	}
}

public class CircleDepB
{
	public CircleDepA A { get; private set;}

	public CircleDepB() {}
	
	public CircleDepB(CircleDepA a)
	{
		A = a;
	}
}

class MyIocTests : UnitTestBase
{
	[Test]
	public void Resolve_Unregistered_Type_Returns_Null()
	{
		var ioc = new MyDI();
		
		var instance = ioc.Resolve<IFoo>();
		
		Assert.IsTrue(instance == null);
	}
	
	[Test]
	public void Register_Resolve_Instance()
	{		
		var ioc = new MyDI();
		
		var foo = new Foo() { Test = "test" };
		ioc.Register<IFoo, Foo>(foo);
			
		var instance = ioc.Resolve<IFoo>();
	
		Assert.IsTrue(instance == foo);
	}
	
	[Test]
	public void Register_Resolve_Type_Singleton()
	{
		var ioc = new MyDI();
		
		ioc.Register<IFoo, Foo>(true);
		
		var instance1 = ioc.Resolve<IFoo>();
		var instance2 = ioc.Resolve<IFoo>();
		
		Assert.IsTrue(instance1 == instance2);
	}
	
	[Test]
	public void Register_Resolve_Type_PerInstance()
	{
		var ioc = new MyDI();
		
		ioc.Register<IFoo, Foo>();
		
		var instance1 = ioc.Resolve<IFoo>();
		var instance2 = ioc.Resolve<IFoo>();
		
		Assert.IsFalse(instance1 == instance2);
	}
	
	[Test]
	public void Constructor_Injection_Resolve_Instance()
	{
		var ioc = new MyDI();
		
		var foo = new Foo();
		ioc.Register<IFoo, Foo>(foo);
		ioc.Register<IBar, Bar>();
		
		var bar = ioc.Resolve<IBar>();
		
		Assert.IsTrue(bar.Foo == foo);
	}

	[Test]
	public void Constructor_Injection_Resolve_PerInstance()
	{
		var ioc = new MyDI();

		ioc.Register<IFoo, Foo>();
		ioc.Register<IBar, Bar>();

		var foo = ioc.Resolve<IFoo>();
		var bar = ioc.Resolve<IBar>();

		Assert.IsTrue(bar.Foo != null);
		Assert.IsFalse(bar.Foo == foo);
	}

	[Test]
	public void Constructor_Injection_Resolve_Singleton()
	{
		var ioc = new MyDI();

		ioc.Register<IFoo, Foo>(true);
		ioc.Register<IBar, Bar>();

		var bar = ioc.Resolve<IBar>();

		Assert.IsTrue(bar.Foo != null);
		
		var foo = ioc.Resolve<IFoo>();
		Assert.IsTrue(bar.Foo == foo);
	}
	
	[Test]
	public void Constructor_Injection_Not_Needed()
	{
		var ioc = new MyDI();
		
		ioc.Register<IBar, Bar>();
		var bar = ioc.Resolve<IBar>();
		
		Assert.IsTrue(bar.Foo == null);
	}
	
	[Test]
	public void Constructor_Injection_Use_Greediest()
	{
		var ioc = new MyDI();

		ioc.Register<Zoo, Zoo>();
		ioc.Register<IBar, Bar>();
		ioc.Register<IFoo, Foo>();

		var zoo = ioc.Resolve<Zoo>();

		Assert.IsFalse(zoo.Foo == null);
		Assert.IsFalse(zoo.Bar == null);
	}
	
	[Test]
	public void Constructor_Injection_Circular_Dependency_Throws()
	{
		var ioc = new MyDI();
		
		ioc.Register<CircleDepA, CircleDepA>();
		ioc.Register<CircleDepB, CircleDepB>();

		Assert.ThrowsException( () => ioc.Resolve<CircleDepA>() );		
	}
}


// Define other methods and classes here
public class MyDI
{
	Dictionary<Type, Func<object>> instances = new Dictionary<Type, Func<object>>();

	public void Register<TK, T>(T instance) where T : TK
	{
		var key = typeof(TK);
		instances.Add(key, () => instance);
	}
	
	public void Register<TK, T>(bool singleton = false) where T : TK, new()
	{
		var key = typeof(TK);
		var instanceConstructor = GreedyConstructor<T>();

		if (singleton)
		{
			var lazy = new Lazy<object>(instanceConstructor);
			instanceConstructor = () => lazy.Value;
		}
		
		instances.Add(key, new Func<object>(instanceConstructor));
	}
	
	public T Resolve<T>()
	{
		return (T)Resolve(typeof(T));
	}
	
	object Resolve(Type type)
	{		
		return instances.ContainsKey(type) 
				? instances[type]()
				: null;
	}
	
	Func<object> GreedyConstructor<T>() where T : new()
	{
		var type = typeof(T);
		
		var constructors = type.GetConstructors();
			
		return () =>
		{
			var useThis = (from c in constructors
						  let p = c.GetParameters().Where(x => instances.ContainsKey(x.ParameterType)).Count()
						  orderby p descending
						  select c).First();

			// shortcut to avoid reflection for simple types
			if (useThis.GetParameters().Length == 0)
				return new T();

			var args = useThis.GetParameters().Select(p => Resolve(p.ParameterType)).ToArray();
			return useThis.Invoke(args);
		};
	}
}









	
// test framework initially based on http://www.youtube.com/watch?feature=player_detailpage&list=PL3D3F4B7C71FF6AA0&v=hayjhjIKSwA
[AttributeUsage(AttributeTargets.Method)]
class SetupAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
class TestAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
class TeardownAttribute : Attribute
{
}

class Assert
{
	public bool Passed { get; set; }
	public string Message { get; private set; }
	
	public Assert()
	{
		Reset();
	}
	
	public void Reset()
	{
		Passed = true;
		Message = string.Empty;
	}
	
	public void AreEqual(double expected, double actual)
	{
		if(!expected.Equals(actual))
		{
			Passed = false;
			Message = string.Format("Expected {0}, but was {1}.", expected, actual);
		}
	}
	
	public void AreEqual(bool expected, bool actual)
	{
		if(!expected.Equals(actual))
		{
			Passed = false;
			Message = string.Format("Expected {0}, but was {1}.", expected, actual);
		}
	}
	
	public void IsTrue(bool actual)
	{
		AreEqual(true, actual);
	}
	
	public void IsFalse(bool actual)
	{
		AreEqual(false, actual);
	}
	
	public void ThrowsException(Action method)
	{
		try 
		{
			method();
		}
		catch
		{
			Passed = true;
		}
		Passed = false;
		Message = "Expected an exception to be thrown";
	}
	
	public void WriteResults(string methodName)
	{
		if(Passed)
			Console.WriteLine("Success: {0}", methodName);
		else
			Console.WriteLine("Failed: {0} - {1}", methodName, Message);
	}
}


abstract class UnitTestBase
{
	protected Assert Assert {get; private set;}
	
	public UnitTestBase()
	{
		Assert = new Assert();
	}
	
	public void RunTests()
	{
		// run Setup methods
		var methods = this.GetType().GetMethods();
		foreach (var method in methods.Where(m => m.IsDefined(typeof(SetupAttribute), false)))
		{
			this.GetType().InvokeMember(method.Name, BindingFlags.InvokeMethod, null, this, null);
		}
		
		// run Test methods
		foreach (var method in methods.Where(m => m.IsDefined(typeof(TestAttribute), false)))
		{
			// clear results
			Assert.Reset();
			
			// run the test
			this.GetType().InvokeMember(method.Name, BindingFlags.InvokeMethod, null, this, null);
			
			// report results
			Assert.WriteResults(method.Name);
		}
		
		// run Teardown methods
		foreach (var method in methods.Where(m => m.IsDefined(typeof(TeardownAttribute), false)))
		{
			this.GetType().InvokeMember(method.Name, BindingFlags.InvokeMethod, null, this, null);
		}
	}
}