# Vvr.System

*Part of **Vvr Framework**®  
Copyright 2024 Syadeu. All rights reserved* 

------

이 시스템은 MVPC, Model, View, Provider(Presenter), Controller 으로 설계되었습니다. 

Model은 게임에서의 가장 기초적인 데이터를 다루고 있습니다. 예를 들어, [AbnormalSheet](Model/AbnormalSheet.cs)와 같이 기획에서 구성된 데이터를 기초 가공하여 저장합니다.

Provider는 Model에게서 제공받은 데이터를 Controller에게 공급합니다.

Controller는 최종적으로 데이터를 가공하여 View에게 제공합니다.

------

## Model

Excel sheet를 기반으로 설계하여 기획자의 요구사항을 맞추고, 게임내 다양한 요소들에 대해 직간접적인 조작이 가능하도록 설계되었습니다.

| Cancellation    |       |             |                | AbnormalChain |      |      |
| --------------- | ----- | ----------- | -------------- | ------------- | ---- | ---- |
| Condition       | Value | Probability | ClearAllStacks | 1             | 2    | 3    |
| OnAbnormalAdded |       | 100         | FALSE          | S0000         |      |      |

위는 이상현상을 관리하는 데이터 시트 중 일부입니다. AbnormalChain 은 기획 필요에 의해 3개를 초과하는 값이 필요하더라도 프로그래머의 개입없이 행을 추가하기만 하면 자동으로 확장되는 구조를 갖고 있습니다. 

| Duration  |      | TimeCondition |      |        |
| --------- | ---- | ------------- | ---- | ------ |
| DelayTime | Time | 1             | 2    | 3      |
|           | 10   | Always        | AND  | Always |

지속시간에 대한 제어도 프로그래머의 별도 개입없이 가능하도록 설계되었습니다. TimeCondition 또한 동적으로 배열이 할당될 수 있으며, OR, AND 게이트 사용으로 게임의 필수 컨디션들을 연산할 수 있습니다.

| TimeCondition |      |          |
| ------------- | ---- | -------- |
| 1             | 2    | 3        |
| HasPassive    | AND  | IsInHand |

다음과 같이 아무 패시브를 갖고있고, 손에 들고있다면 지속시간이 차감될 수 있다는 의미로 만들어질 수 있습니다.

이러한 [Condition](Model/Condition.cs)(이하 조건)들은 시스템 내에서 시간 단위로 관리되며. 조건은 게임의 볼륨에 따라 매우 많이 증가할 수 있어(+64개 그 이상) Bitmask 형태를 고려하지 않았으므로, 추가적인 그룹 구조가 필요하였습니다. 이를 해결하기 위해 [ConditionQuery](Model/ConditionQuery.cs)를 설계하여 발생한 이벤트들을 최대 64개 단위로 묶어 Controller가 이를 확인할 수 있습니다.

조건들은 순차 정수로 정의되어 비트 마스킹이 불가능하기 때문에 여러 조건들을 한번에 검사하는 것은 이것만으로는 불가능합니다. 이를 일일이 검사하는 것은 무의미한 시간낭비이고, 연산 낭비에 속하기 때문에 비트 마스킹을 가능하도록 하는 쿼리를 여러 연산자를 오버로딩하여 알맞게 사용할 수 있도록 설계하였습니다.

```C#
public static implicit operator ConditionQuery(Condition c) => new ConditionQuery((short)c, 1);
```

단일 조건을 먼저 쿼리로 변환할 수 있도록하고,

```C#
public static ConditionQuery operator |(ConditionQuery x, ConditionQuery y)
{
    short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
    long xf = x.m_Filter << math.abs(o - x.m_Offset),
        yf  = y.m_Filter << math.abs(o - y.m_Offset);

    return new ConditionQuery(o, xf | yf);
}
```

여러 조건들을 병합할 수 있도록 | 연산자를 오버로딩합니다.

```C#
public static ConditionQuery operator &(ConditionQuery x, ConditionQuery y)
{
    short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
    long xf = x.m_Filter << math.abs(o - x.m_Offset),
        yf  = y.m_Filter << math.abs(o - y.m_Offset);

    return new ConditionQuery(o, xf & yf);
}
```

그리고 & 연산자를 통해 해당 조건들이 존재하는지 한번에 확인할 수 있습니다.

```c#
[Test]
public void Test_4()
{
    ConditionQuery query = Condition.GEqual;
    query |= Condition.OnHit;
    query |= Condition.HasAbnormal;
    query |= Condition.HasPassive;
    query |= Condition.IsInHand;
    query |= Condition.IsPlayerActor;

    ConditionQuery targetQuery = Condition.HasPassive;
    targetQuery |= Condition.HasAbnormal;
    targetQuery |= Condition.OnActorDead;

    query &= targetQuery;

    Assert.AreEqual(2, query.Count);
    Assert.IsTrue(query.Has(Condition.HasPassive));
    Assert.IsTrue(query.Has(Condition.HasAbnormal));

    Assert.IsFalse(query.Has(Condition.GEqual));
    Assert.IsFalse(query.Has(Condition.OnHit));
    Assert.IsFalse(query.Has(Condition.OnActorDead));
    Assert.IsFalse(query.Has(Condition.IsInHand));
    Assert.IsFalse(query.Has(Condition.IsPlayerActor));
}
```

| Stats |      |      |      |      |
| ----- | ---- | ---- | ---- | ---- |
| SPD   | ATT  | DEF  | ARM  | HP   |
| 10    | 20   | 5    | 2    | 300  |

Stat(이하 스탯)또한 기획 필요로 인해 새로운 스탯이 추가되어도 이를 별도 스탯 데이터 시트에 추가하고, 해당 ID 를 입력하면 게임 내에서 동적으로 해결되도록 설계하였습니다. 이를 가능하게 하기 위해 시트에서 값을 가져오는 [UnresolvedStatValues](Model/Stat/UnresolvedStatValues.cs)와 스탯 데이터 시트를 통해 해결된 값들을 담는 [StatValues](Model/Stat/StatValues.cs)로 설계되었습니다.

스탯은 최대 64개를 기획자가 개발할 수 있도록 설계되었으며, 이때 시스템에서 직접 개입하는 스탯에 대해서는 [StatType](Model/Stat/StatType.cs)으로 관리하며, 중요 시스템에서 사용되는 스탯 값이 아닌 스탯 값들은 Unknown 스탯 값으로 구분되어 long 값을 통해 시스템에서 동적으로 추론하여 스탯을 참조하고 할당합니다.

중요 스탯 이외는 프로그래머가 인지하지 못하는 값으로 시스템에 존재하므로, 연산을 위해 다양한 Operator 를 추가하였습니다. [StatValues](Model/Stat/StatValues.cs)는 존재하는 스탯만을 위한 배열을 생성하는데 (예를 들어 HP | MP = 길이 2의 배열) 이를 각 코드에서 프로그래머가 제어하는 것은 대단히 위험하고 복잡한 작업이 될 것입니다.

```C#
public static StatValues operator |(StatValues x, StatType t)
{
    if ((x.Types & t) == t) return x;

    var result   = Create(x.Types | t);
    int maxIndex = result.Values.Count;
    for (int i = 0, c = 0, xx = 0; i < 64 && c < maxIndex; i++)
    {
        var e = 1L << i;
        if (((long)result.Types & e) == 0) continue;

        if (((long)x.Types & e) != 0) result.m_Values[c] = x.m_Values[xx++];
        c++;
    }
    return result;
}
public static StatValues operator +(StatValues x, IReadOnlyStatValues y)
{
    if (y?.Values == null) return x;

    var newTypes = (x.Types | y.Types);
    var result   = (x.Types & y.Types) != y.Types ? Create(x.Types | y.Types) : x;

    int maxIndex = result.Values.Count;
    for (int i = 0, c = 0, xx = 0, yy = 0; i < 64 && c < maxIndex; i++)
    {
        var e = 1L << i;
        if (((long)newTypes & e) == 0) continue;

        if (((long)x.Types & e) != 0) result.m_Values[c] =  x.m_Values[xx++];
        if (((long)y.Types & e) != 0) result.m_Values[c] += y.Values[yy++];
        c++;
    }

    return result;
}
```

이를 방지하기 위한 연산자를 오버로딩하고, 상황에 알맞게 연산하도록 하였습니다.

```c#
[Test]
public void UnknownTypeTest()
{
    StatType unknownType1 = (StatType)(1L << 50);
    StatType unknownType2 = (StatType)(1L << 40);
    StatType unknownType3 = (StatType)(1L << 35);
    StatValues
        x = StatValues.Create(unknownType1 | unknownType2 | unknownType3);

    x[unknownType1] = 10;
    x[unknownType2] = 506;
    x[unknownType3] = 123124;

    Assert.IsTrue(Mathf.Approximately(10, x[unknownType1]),
        $"{x[unknownType1]}");
    Assert.IsTrue(Mathf.Approximately(506, x[unknownType2]),
        $"{x[unknownType2]}");
    Assert.IsTrue(Mathf.Approximately(123124, x[unknownType3]),
        $"{x[unknownType3]}");
}
```

이렇게 프로그래머에 의해 정의되지 않은 스탯에 대해서도 값을 해결할 수 있고,

```c#
[Test]
public void AndOperatorTest_3()
{
    StatValues
        x = StatValues.Create(StatType.HP);

    x[StatType.HP] = 10;

    x |= StatType.ARM;
    x |= StatType.SPD;

    Assert.AreEqual(StatType.HP | StatType.ARM | StatType.SPD, x.Types);
    Assert.AreEqual(3, x.Values.Count);

    Assert.IsTrue(Mathf.Approximately(10, x[StatType.HP]), $"{x[StatType.HP]}");

    x[StatType.ARM] = 100;
    x[StatType.SPD] = 850;

    Assert.IsTrue(Mathf.Approximately(100, x[StatType.ARM]), $"{x[StatType.ARM]}");
    Assert.IsTrue(Mathf.Approximately(850, x[StatType.SPD]), $"{x[StatType.SPD]}");
}
```

```c#
[Test]
public void PlusOperatorTest_3()
{
    StatValues
        x = StatValues.Create(StatType.HP  | StatType.ATT),
        y = StatValues.Create(StatType.ARM | StatType.HP | StatType.DEF | StatType.ATT);

    x[StatType.HP]  = 10;
    x[StatType.ATT] = 50;

    y[StatType.HP]  = 90;
    y[StatType.ATT] = 50;
    y[StatType.ARM] = 200;

    StatValues result = x - y;
    Assert.IsTrue(Mathf.Approximately(-80, result[StatType.HP]), $"{result[StatType.HP]}");
    Assert.IsTrue(Mathf.Approximately(0, result[StatType.ATT]), $"{result[StatType.ATT]}");
    Assert.IsTrue(Mathf.Approximately(-200, result[StatType.ARM]), $"{result[StatType.ARM]}");
    Assert.IsTrue(Mathf.Approximately(0, result[StatType.DEF]), $"{result[StatType.DEF]}");
}
```

다음과 같이 유동적으로 값을 수정할 수 있게 설계하였습니다.

------

## Provider

[Provider](Provider/Provider.cs)는 값을 제공받고, 연결된 모든 [IConnector](Provider/IConnector.cs)에게 값을 보장하는 역할을 합니다. Model로부터 데이터를 받으면 즉시 연결된 모든 Connector(대부분 Controller)에게 값을 전달하는 방식과 필요에 의해 값을 보장받을 수 있는 Lazy 로 구분됩니다.

이벤트 객체를 직접적으로 알고있거나, 조건에 대해 제공할 의무가 있는 Controller를 위해 설계되었습니다. [DefaultStage](Controller/Session/World/DefaultStage.cs)(이하 스테이지) 객체는 스테이지에 대한 모든 액터에 대해 제공할 의무가 있는 설계상 가장 하위 Session(이하 세션)입니다. 그래서 스테이지 객체는 [IGameMethodProvider](Controller/Provider/IGameMethodProvider.cs), [ITargetProvider](Controller/Provider/ITargetProvider.cs), [IStateConditionProvider](Provider/IStateConditionProvider.cs)를 상속받고, 요청에 맞는 각 메서드를 제공합니다. 예를 들어, [GameConfigSheet](Model/GameConfigSheet.cs)에서 정의된 조건에 맞는 메서드를 생성하여 반환합니다.

```C#
GameMethodImplDelegate IGameMethodProvider.Resolve(GameMethod method)
{
  if (method == GameMethod.Destroy)
  {
    return async e =>
    {
      if (m_DestroyProcessing) return;
      if (e is not IActor x) return;

      m_DestroyProcessing = true;
      using (var trigger = ConditionTrigger.Push(x, nameof(GameMethod)))
      {
        await trigger.Execute(Condition.OnActorDead, null);
      }

      var field = x.ConditionResolver[Condition.IsPlayerActor](null) ? m_PlayerField : m_EnemyField;
      int index = field.FindIndex(e => e.owner == x);
      if (index < 0)
      {
        $"{index} not found in field {x.ConditionResolver[Condition.IsPlayerActor](null)}".ToLogError();
        return;
      }

      RuntimeActor actor = field[index];

      $"Actor {actor.owner.DisplayName} is dead {actor.owner.Stats[StatType.HP]}".ToLog();

      await Delete(field, actor);
      m_DestroyProcessing = false;
    };
  }

  throw new NotImplementedException();
}
```

### EventTarget

조건을 발생할 수 있는 모든 객체는 [IEventTarget](Provider/IEventTarget.cs)을 상속받고, 가장 기초 레벨의 데이터를 제공할 의무를 갖습니다. 

```C#
public interface IEventTarget
{
    /// <summary>
    /// Server level unique owner id
    /// </summary>
    Owner Owner { get; }
    string DisplayName { get; }
    bool   Disposed    { get; }
}
```

[Owner](Provider/Owner.cs)를 통해 각 클라이언트를 구분하거나 AI 를 구분지을 수 있습니다.

------

## Controller

Controller는 시스템 내에서 최종적인 데이터 가공이 이루어지고, 실제 View 에게 공급하는 역할을 합니다. 사용자에 의해 정의된 모든 이벤트 주체는 [IEventTarget](Provider/IEventTarget.cs)을 통해 모든 조건들에 대해 검사할 수 있으며, 사용자에 의해 정의된 플레이어 객체 [IActor](Controller/Actor/IActor.cs)(이하 액터) 만을 위한 조건도 존재합니다. 이는 조건을 제공하는 Controller에서 Provider에게 해당 조건을 제공하고, [SkillController](Controller/Skill/SkillController.cs), [AbnormalController](Controller/Abnormal/AbnormalController.cs), [PassiveController](Controller/Passive/PassiveController.cs)등에서 조건들에 대해 [ConditionTrigger](Controller/Condition/ConditionTrigger.cs)로 공급하게 합니다.

### ConditionTrigger

[ConditionTrigger](Controller/Condition/ConditionTrigger.cs)는 게임내 이벤트들에 대한 모든 조건에 대해 발생하였음을 알리는 Event Broadcaster 입니다. 

```C#
public static bool Any(IEventTarget target, Condition condition, string value)
{
    Event e = new Event(condition, value);
    // if value is null, should check only condition
    if (value.IsNullOrEmpty())
    {
        return s_Stack
            .Where(x => x.m_Target == target)
            .SelectMany(x => x.m_Events)
            .Any(x => x.condition == condition);
    }
    return s_Stack
        .Where(x => x.m_Target == target)
        .Any(x => x.m_Events.Contains(e));
}
```

공급받은 value 값은 시트에서 정의된 value 값이거나, OnHit(이 경우, value 값은 히트된 데미지를 의미)와 같이 시스템에서 발생하는 이벤트의 값 입니다. 

소멸자 패턴을 활용하여 이벤트 객체에 대한 전체 Scope(이하 스코프)를 생성하고, 해당 스코프 내에서는 그 이벤트 객체에 대한 조건임을 명시합니다.

```c#
public static ConditionTrigger Push(IEventTarget target, string displayName = null)
{
    Assert.IsFalse(target.Disposed);
    // $"[Condition:{target.Owner}:{target.name}] Push trigger stack depth: {s_Stack.Count}".ToLog();

    string path;
    if (!displayName.IsNullOrEmpty()) path = s_Stack.Count > 0 ? $"{s_Stack[^1].m_Path}->{displayName}" : displayName;
    else path                              = s_Stack.Count > 0 ? $"{s_Stack[^1].m_Path}->{target.GetHashCode()}" : target.GetHashCode().ToString();

    // If last stack is already target, return
    if (s_Stack.Count > 0 && s_Stack[^1].m_Target == target)
    {
        var existing = s_Stack[^1];
        return new ConditionTrigger(existing, path);
    }

    var t = new ConditionTrigger(target, path);
    s_Stack.Add(t);
    return t;
}
```

이렇게 스코프를 형성하면 다음과 같이 사용될 수 있습니다.

```C#
using (var trigger = ConditionTrigger.Push(currentRuntimeActor.owner, ConditionTrigger.Game))
{
    await trigger.Execute(Condition.OnActorTurn, null);

    ExecuteTurn(currentRuntimeActor).Forget();

    await m_ResetEvent.Task;
}
```

해당 액터에 대한 조건임을 명시하고, 하위 모든 트리거에 대한 이벤트의 시작점이 해당 액터임을 명시합니다. 이는 액터의 행동 시간 중 다른 액터의 행동 또한 발생할 수 있기 때문인데, 예를 들어 스킬을 사용한 후, 피격당한 액터가 다시 반격을 하거나, 또는 그 이전에 OnHit 조건을 발생시킬수 있기 떄문에 모든 이벤트에 대해 소유자를 명시하여야합니다.

이렇게 발생한 조건들에 대해, 그 조건을 소유(즉, 이 이벤트 객체의 시간내에서 조건이 발생한 적 있는지)를 검사할 수 있고, 그 외 필요한 모든 조건에 대해 검사할 수 있는 [ConditionResolver](Controller/Condition/ConditionResolver.cs)를 설계하였습니다.

### ConditionResolver

Provider 구조체는 전역에 대해 제공할 의무가 있다면, [ConditionResolver](Controller/Condition/ConditionResolver.cs)는 지역에 대해서만 제공할 의무가 있는 객체입니다. 즉, 모든 이벤트 객체는 [ConditionResolver](Controller/Condition/ConditionResolver.cs)를 소유할 수 있으며, 자신에 대한 값을 제공하는 [IProvider](Provider/IProvider.cs)들을 연결하여 한번에 값을 해결할 수 있습니다.

만약, 소유한 이벤트 객체가 액터이고, 액터임으로 스탯을 보유한다고 가정할 때, 이 [ConditionResolver](Controller/Condition/ConditionResolver.cs)를 통해 특정 스탯에 대해 값을 해결 할 수 있습니다.

```C#
public ConditionResolver Connect(IStatValueStack stats, IStatConditionProvider provider)
{
    Assert.IsTrue(m_Owner is IActor);

    var conditions
        = Enum.GetValues(typeof(OperatorCondition)).Cast<OperatorCondition>();
    foreach (var condition in conditions)
    {
        this[(Condition)condition] = x => provider.Resolve(stats.OriginalStats, stats, condition, x);
    }
    return this;
}
```

이렇게 연결되면 해당 컨디션에 대해 해결할 의무는 온전히 Provider, 즉 여기서는 [IStatConditionProvider](Provider/IStatConditionProvider.cs)에게 이관됩니다. 이 인터페이스를 상속받는 [StatProvider](Provider/StatProvider.cs)는 시트에서 문자열로 입력받은 값에 대해 파싱하고, 검증하여 반환하도록 보장합니다.

연결된 스탯은 델리게이트 생성자를 통해 간접 참조를 수행할 수 있습니다.

```C#
public delegate float StatValueGetterDelegate(in IReadOnlyStatValues stat);
public delegate void StatValueSetterDelegate(in StatValues stat, float value);
```

```C#
public static StatValueGetterDelegate GetGetMethod(StatType t)
{
    if (!s_CachedGetter.TryGetValue(t, out var d))
    {
        d                 = (in IReadOnlyStatValues x) => x[t];
        s_CachedGetter[t] = d;
    }
    return d;
}
public static StatValueSetterDelegate GetSetMethod(StatType t)
{
    if (!s_CachedSetter.TryGetValue(t, out var d))
    {
        d              = (in StatValues x, float value) => x.SetValue(t, value);
        s_CachedSetter[t] = d;
    }

    return d;
}
```

스탯 타입(프로그램에 정의되지 않은 값도 가능)으로 해당 스탯 타입으로 연결하는 델리게이트를 얻을 수 있고, 이것을 통해 각 Controller의 기능 수행부분은 스탯이 무엇인지 알지못해도 알맞는 스탯에 대해 연산을 수행할 수 있습니다.

```C#
void IStatModifier.UpdateValues(in IReadOnlyStatValues originalStats, ref StatValues stats)
{
    foreach (var e in m_Values.OrderBy(ValueMethodOrderComparer.Selector, ValueMethodOrderComparer.Static))
    {
        int length = e.updateCount;
        for (int i = 0; i < length; i++)
        {
            e.abnormal.setter(stats, e.abnormal.method(
                e.abnormal.getter(stats),
                e.abnormal.value
            ));
        }
    }
    m_IsDirty = false;
}
```

### Session

이 시스템을 기반으로하는 게임은 세션을 통해 관리됩니다. 세션의 종류는 크게 3가지로 나뉘며, 각 종류에 맞는 역할을 부여받습니다.

#### [ChildSession](Controller/Session/ChildSession.cs)

최하위 세션입니다. 자식 세션은 다른 자식을 가지거나 다른 세션에 대해 관리할 권한이 없습니다.

#### [ParentSession](Controller/Session/ParentSession.cs)

부모 세션은 다른 자식을 갖고, 관리할 권한과 의무를 갖습니다.

#### [RootSession](Controller/Session/RootSession.cs)

최상단 세션입니다. 직접적으로 연결된 자식에 대해서만 관리할 권한과 의무를 갖습니다.

이 세션의 구조를 사용하여 World를 구성하고, 하위 구조를 설계할 수 있습니다. 아래는 현재 기본으로 구성한 세션의 구조입니다.

[DefaultWorld](Controller/Session/World/DefaultWorld.cs) -> [DefaultMap](Controller/Session/World/DefaultMap.cs) -> [DefaultRegion](Controller/Session/World/DefaultRegion.cs) -> [DefaultFloor](Controller/Session/World/DefaultFloor.cs) -> [DefaultStage](Controller/Session/World/DefaultStage.cs)

World는 [GameWorld](Controller/Session/World/GameWorld.cs)를 통해 생성될 수 있으며, 각 계층 구조에 맞게 생성되어야합니다. 이와 같은 설계를 갖게된 것은 각 세션 Depth 에 맞춰서 설정된 이상현상을 부여할 수 있기 때문인데, 예를 들어 어떤 패시브는 Region(이하 지역)내에서만 활성화되는 기획일 수 있기 때문입니다. 이 경우, Floor, Stage와 상관없이 해당 지역의 세션에 액터가 존재한다면 활성화될 수 있습니다.

