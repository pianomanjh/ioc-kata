<Query Kind="Program" />

void Main()
{
	var tests = new MyIocTests();
	tests.RunTests();
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

class MyIocTests : UnitTestBase
{
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
}


// Define other methods and classes here
public class MyDI
{
	Dictionary<Type, object> instances = new Dictionary<Type, object>();

	public void Register<TK, T>(T instance) where T : TK
	{
		var key = typeof(TK);
		if(instances.ContainsKey(key))
			throw new InvalidOperationException(key.ToString() + "is already registered");
			
		instances.Add(key, instance);
	}
	
	public void Register<TK, T>(bool singleton = false) where T : TK, new()
	{
		var key = typeof(TK);
		var instanceConstructor = GreedyConstructor<TK, T>();		

		if (singleton)
		{
			var lazy = new Lazy<TK>(instanceConstructor);
			instanceConstructor = () => lazy.Value;
		}
		
		instances.Add(key, new Func<TK>(instanceConstructor));
	}
	
	public T Resolve<T>()
	{
		var value = instances[typeof(T)];
		if(value is Func<T>)
			return ((Func<T>)value)();
		else
			return (T)value;	
	}
	
	private object Resolve(Type type)
	{
		var value = instances[type];
		if(type.IsAssignableFrom(value.GetType()))
			return value;
		else
			return ((Func<object>)value)();
	}
	
	Func<TK> GreedyConstructor<TK, T>() where T : TK, new()
	{
		var type = typeof(T);
		
		var constructors = type.GetConstructors();
			
		var useThis = (from c in constructors
					  let p = c.GetParameters().Where(x => instances.ContainsKey(x.ParameterType)).Count()
					  orderby p descending
					  select c).First();

		// shortcut to avoid reflection / boxing for simple types
		if (useThis.GetParameters().Length == 0)
			return () => new T();
			
		return () => (TK)useThis.Invoke(useThis.GetParameters().Select (p => Resolve(p.ParameterType)).ToArray());						
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