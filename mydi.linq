<Query Kind="Program">
  <NuGetReference>Castle.Windsor</NuGetReference>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>Castle.MicroKernel.Registration</Namespace>
</Query>

void Main()
{
	var tests = new MyIocTests();
	tests.RunTests(true);
	
	/* DI Container kata
	1. ability to register an instance, and ensure it resolves
	2. a container only resolves types that were registered
	3. ability to register a type. Ensure it resolves
	4. ability to register a singleton.  Each resolve return same instance	
	5. DI: ability to resolve a type with a constructor of one parameter of a registered type
	6. DI: can register types in any order to get valid resolve
	7. DI: ability to resolve a type using the 'greediest' constructor (one with most registered types)
	8. DI: ensure a circular dependency throws an exception (instead of stack overflow)
	9. DI: make it thread safe
	10. perf?
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

public class FooProp
{
	public IBar Bar { get; set; }
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
	public CircleDepC C { get; private set; }
	public IFoo Foo { get; private set;}

	public CircleDepB() {}
	
	public CircleDepB(IFoo foo, CircleDepC c)
	{
		Foo = foo;
		C = c;
	}
}

public class CircleDepC
{
	public CircleDepA A { get; private set; }

	public CircleDepC() { }

	public CircleDepC(CircleDepA a)
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
		ioc.Register<CircleDepC, CircleDepC>();
		ioc.Register<IFoo, Foo>();

		Assert.ThrowsException( () => ioc.Resolve<CircleDepA>() );		
	}
	
	//[Test]
	public void Thread_Safety_3_Threads_of_10k_Resolves()
	{
		var ioc = new MyDI();

		ioc.Register<Zoo, Zoo>();
		ioc.Register<IBar, Bar>();
		ioc.Register<IFoo, Foo>();

		var mre = new ManualResetEvent(false);
		var t1 = new Thread(() => RegisterResolve(ioc, mre));
		var t2 = new Thread(() => RegisterResolve(ioc, mre));
		var t3 = new Thread(() => RegisterResolve(ioc, mre));
		
		t1.Start();
		t2.Start();
		t3.Start();

		mre.Set();
		
		t1.Join();
		t2.Join();
		t3.Join();
	}

	private void RegisterResolve(MyDI ioc, ManualResetEvent ev)
	{
		ev.WaitOne();

		//int count = new Random().Next(500,10000);
		for (int i = 0; i < 10000; i++)
		{
			ioc.Resolve<Bar>();
			ioc.Resolve<Zoo>();
			ioc.Resolve<Foo>();
		}
	}

	//[Test]
	public void Thread_Safety_3_Threads_of_10k_Resolves_CastleWindsor()
	{
		var ioc = new Castle.Windsor.WindsorContainer();
		
		ioc.Register(Component.For<Zoo>().LifestyleTransient());
		ioc.Register(Component.For<IBar, Bar>().LifestyleTransient());
		ioc.Register(Component.For<IFoo, Foo>().LifestyleTransient());

		var mre = new ManualResetEvent(false);
		var t1 = new Thread(() => RegisterResolve(ioc, mre));
		var t2 = new Thread(() => RegisterResolve(ioc, mre));
		var t3 = new Thread(() => RegisterResolve(ioc, mre));

		t1.Start();
		t2.Start();
		t3.Start();

		mre.Set();

		t1.Join();
		t2.Join();
		t3.Join();
	}

	private void RegisterResolve(Castle.Windsor.WindsorContainer ioc, ManualResetEvent ev)
	{
		ev.WaitOne();

		//int count = new Random().Next(500,10000);
		for (int i = 0; i < 10000; i++)
		{
			ioc.Resolve<Bar>();
			ioc.Resolve<Zoo>();
			ioc.Resolve<Foo>();
		}
	}
	
	[Test]
	public void Test_PropertyInjection()
	{
		var ioc = new MyDI();

		ioc.Register<Zoo, Zoo>();
		ioc.Register<IBar, Bar>();
		ioc.Register<IFoo, Foo>();
		
		var foo = ioc.Resolve<IFoo>();
		
		Assert.IsTrue(foo.Bar != null);
	}
}


// Define other methods and classes here
public class MyDI
{
	ConcurrentDictionary<Type, Func<HashSet<Type>, object>> instances = new ConcurrentDictionary<Type, Func<HashSet<Type>, object>>();
	bool registrationSinceResolve = false;
	object lockObj = new object();

	public void Register<TK, T>(T instance) where T : TK
	{
		var key = typeof(TK);
		_Register(key, (_) => instance);
	}
	
	public void Register<TK, T>(bool singleton = false) where T : TK, new()
	{
		var key = typeof(TK);
		var instanceConstructor = GreedyConstructor<T>();
		var func = new Func<HashSet<Type>, object>(instanceConstructor);

		if (singleton)
		{
			var lazy = new Lazy<object>(() => instanceConstructor(new HashSet<Type>()));
			func = (_) => lazy.Value;
		}		
		
		_Register(key, func);
	}
	
	private void _Register(Type key, Func<HashSet<Type>, object> func)
	{
		instances.AddOrUpdate(key, func, (type, func2) => func);
		lock (lockObj) { registrationSinceResolve = true; }
	}
	
	public T Resolve<T>()
	{
		return (T)Resolve(typeof(T), new HashSet<Type>());
	}
	
	object Resolve(Type type, HashSet<Type> circularDependencyTracker)
	{
		if (instances.ContainsKey(type))
		{
			var instance = instances[type](circularDependencyTracker);
			var props = (from p in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
						where instances.ContainsKey(p.PropertyType) && p.CanWrite
						select p);
			foreach(var prop in props)
				prop.SetValue(instance, instances[prop.PropertyType](circularDependencyTracker));
				
			return instance;
		}		
		return null;
	}
	
	ConcurrentDictionary<Type, ConstructorInfo> constructorCache = new ConcurrentDictionary<Type, ConstructorInfo>();
	
	Func<HashSet<Type>, object> GreedyConstructor<T>() where T : new()
	{
		var type = typeof(T);
		
		var constructors = type.GetConstructors();
			
		return (circularDependencyTracker) =>
		{
			ConstructorInfo constructor;
			if (registrationSinceResolve || !constructorCache.TryGetValue(type, out constructor))
			{
				constructor = (from c in constructors
							   let p = c.GetParameters().Where(x => instances.ContainsKey(x.ParameterType)).Count()
							   orderby p descending
							   select c).First();
				constructorCache.AddOrUpdate(type, (_) => constructor, (_, __) => constructor);

				lock (lockObj) { registrationSinceResolve = false; }
			}
			
			if (!circularDependencyTracker.Add(type))
				throw new InvalidOperationException(string.Format("Circular dependency detected {0}", type));

			var parameters = constructor.GetParameters();

			var instance = parameters.Length == 0
				? new T() // shortcut to avoid reflection for simple types
				: constructor.Invoke(parameters.Select(p => Resolve(p.ParameterType, circularDependencyTracker)).ToArray());
			
			circularDependencyTracker.Remove(type);
			return instance;
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
			Passed = false;
			Message = "Expected an exception to be thrown";
		}
		catch
		{
			Passed = true;
		}
	}
	
	public void WriteResults(string methodName, TimeSpan? duration = null)
	{
		if(Passed)
			Console.WriteLine(duration.HasValue ? "Success: {0} | {1}" : "Success: {0}", methodName, duration);
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
	
	public void RunTests(bool reportDuration = false)
	{
		// run Setup methods
		var methods = this.GetType().GetMethods();
		foreach (var method in methods.Where(m => m.IsDefined(typeof(SetupAttribute), false)))
		{
			this.GetType().InvokeMember(method.Name, BindingFlags.InvokeMethod, null, this, null);
		}
		
		Stopwatch watch = new Stopwatch();
		
		// run Test methods
		foreach (var method in methods.Where(m => m.IsDefined(typeof(TestAttribute), false)))
		{
			// clear results
			Assert.Reset();
			
			// run the test
			Func<object> func = () => this.GetType().InvokeMember(method.Name, BindingFlags.InvokeMethod, null, this, null);

			if (reportDuration)
			{
				watch.Restart();
				func();				
			}
			
			func();
			
			// report results
			Assert.WriteResults(method.Name, reportDuration ? watch.Elapsed : (TimeSpan?)null);
		}
		
		// run Teardown methods
		foreach (var method in methods.Where(m => m.IsDefined(typeof(TeardownAttribute), false)))
		{
			this.GetType().InvokeMember(method.Name, BindingFlags.InvokeMethod, null, this, null);
		}
	}
}