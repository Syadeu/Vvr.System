# Vvr.System

*Part of **Vvr Framework**®  
Copyright 2024 Syadeu. All rights reserved* 

------

이 시스템은 MVPC, `Model`, `View`, `Provider`, `Controller(Presenter)` 으로 설계되었습니다. 

`Model`은 게임에서의 가장 기초적인 데이터를 다루고 있습니다. 예를 들어, [AbnormalSheet](Model/AbnormalSheet.cs)와 같이 기획에서 구성된 데이터를 기초 가공하여 저장합니다.

`Provider`는 `Model`에게서 제공받은 데이터를 `Controller`에게 공급합니다. 그리고 `Controller`로부터 제공받은 정보를 `View`에 제공합니다.

`Controller`는 최종적으로 데이터를 가공하여 `View`에게 제공합니다.

그리고 이 모든 자원을 최대한으로 활용하는 `Session` 을 통해 생성된 자원들을 관리합니다.

------

## Session

이 시스템을 기반으로하는 게임은 세션을 통해 관리됩니다. 세션의 종류는 크게 3가지로 나뉘며, 각 종류에 맞는 역할을 부여받습니다. 

세션은 DI(Dependency Injection) 컨테이너의 기능을 수행합니다. 그러나 전통적인 의미의 DI 컨테이너와는 약간 다른 측면이 존재합니다. 동일한 종류의 여러 세션을 관리하고, 각각에 대해 동일한 종류의 제공자가 등록될 수 있기 때문에, 이는 마치 각 세션마다 독립적인 DI 컨테이너가 있는 것처럼 작동합니다. 따라서 이는 **Session-specific DI**라고 볼 수 있습니다.

각 세션마다 자신이 필요로 하는 의존성들을 가질 수 있으며, 이 의존성들은 세션의 수명 주기 동안 관리됩니다. 이렇게 되면 세션과 세션에 필요한 종속성 사이의 관계가 깔끔하게 유지됩니다. 예를 들어, '로그인' 세션에서는 '사용자 인증' 서비스가 필요할 수 있으며, '게임플레이' 세션에서는 '게임 로직' 서비스가 필요할 수 있습니다. 이런 세션별로 서로 다른 종속성을 가질 수 있게 만들어줍니다.

정리하자면, 세션에서 사용되는 DI 방식은 전통적인 DI 컨테이너가 일반적으로 애플리케이션 전체의 의존성을 관리하는 것과는 다릅니다. 세션 별 의존성 관리라는 보다 구체적이고 특화된 역할을 수행합니다. 이런 특성 덕분에 개발자는 세션 별로 필요한 의존성을 정확하게 관리하면서, 코드의 유지 관리와 확장성도 향상시킬 수 있습니다.

세션의 모든 구현은 객체 지향 설계 중 **SOLID** 원칙으로 코드 베이스에 엄격한 제한을 두고 있습니다. 실제로 개방-폐쇄 원칙(Open-Closed Principle, OCP)으로 세션을 상속받는 `DefaultWorld`는 인터페이스로 `IActorProvider`를 상속받고 있음에도 불구하고, 자기자신을 등록하고 있습니다. 이것은 외부에서 보기에는 다소 불합리하게 보일 수 있습니다. 그러나 이것은 객체 지향의 다형성 원칙을 이용한 것으로, 다른 부분에 자신을 세션이 아닌 `IActorProvider`로 취급할 수 있게 됩니다.

`ChildSession`이 `IActorProvider`의 역할을 하고 싶어할때 그리고 `IActorProvider` 의 인스턴스로서 작동하려고 할 때 효과적인 방법일 수 있습니다. 다시 말해서, `ChildSession`이 `IActorProvider` 인터페이스를 통해 노출하려는 일련의 작업을 결정할 수 있다는 것은 매우 강력한 도구입니다.

왜냐하면 자식 세션 **또한**, 부모에게 강한 의존성을 띄어서는 안되기 때문입니다. 이러한 의존성을 해결하기 위한 방법으로 독특한 방식의 DI 컨테이너가 세션만을 위해 설계되었습니다.

### [ParentSession](Session/ParentSession.cs)

관리하는 하위 세션(`ChildSession`)들을 통해 게임의 동작을 조정하거나, 다른 세션들로부터 정보를 집합하거나, 게임 상태를 업데이트 하는 등의 역할을 수행하게 됩니다.  예를 들어, 실제 게임에서 `ParentSession`은 게임의 한 라운드나 레벨, 게임 플레이 세션 등을 나타낼 수 있으며, 그 아래의 `ChildSession`들은 라운드나 레벨에서 발생하는 각각의 이벤트나 액션 등을 나타내게 됩니다. 

`ParentSession`의 주요 역할 및 특징은 다음과 같습니다.    

1. **세션 계층 관리**: `ParentSession` 객체는 하위 세션(`ChildSession`)들의 집합을 관리합니다. 이를 통해 세션 간의 계층적인 관계를 형성하고 게임의 복잡한 흐름을 관리할 수 있습니다.   
2. **하위 세션 관리**: 하위 세션을 추가, 제거, 관리하는 역할을 수행합니다. 또한, 각 하위 세션의 상태를 관찰하고 그에 따라 알맞은 동작을 취하게 됩니다.
3. **계층적 세션 관리**: 게임의 복잡한 흐름을 관리하기 위해 `ParentSession`은 자신의 하위에 또 다른 `ParentSession`을 가질 수도 있습니다. 이를 통해 세션 사이의 계층적인 관계를 구축하고, 복잡한 게임 로직을 여러 세션 사이로 나누어 관리할 수 있습니다.
4. **게임 상태 통합**: 하위 세션들의 상태를 집합하여 게임의 전반적인 상태를 나타낼 수 있습니다. 예를 들어, 여러 라운드의 결과를 통합해서 게임의 최종 승자를 결정하는 등의 역할을 수행합니다.        
5. **세션 생명주기 관리**:  세션의 생명주기의 시작과 끝을 관리합니다. 세션의 초기화, 진행, 종료등의 생명주기 단계를 제어하고 각 단계에서 필요한 동작을 수행하거나 이벤트를 발생시킵니다.        
6. **세션 데이터 관리**: 연결된 데이터를 관리할 수 있습니다. `ParentSession`의 데이터는 특정 세션 별로 필요한 정보를 유지하고 업데이트합니다. 예를 들어, 게임 라운드의 경우 회차, 처치 횟수, 사용한 아이템 등의 정보가 이 데이터에 포함될 수 있습니다. 이 데이터는 `ParentSession`의 동작 결정에 중요한 역할을 하거나, 게임 UI에 표시될 수 있습니다.
7. **이벤트 전파**: `ParentSession`은 시스템 내에서 발생하는 이벤트를 하위 세션에 전파하는 역할을 수행합니다. 이를 통해 하위 세션에서 해당 이벤트를 바탕으로  필요한 로직을 처리할 수 있게 됩니다. 이렇게 이벤트 전파를 통해 `ParentSession` `ChildSession` 간에 커뮤니케이션을 가능하게 합니다.      
8. **세션 평가**: `ParentSession`은 종종 하위 세션의 성공 여부나 결과를 평가하는 역할을 수행합니다. 이를 통해 게임의 메타 레벨에서 중요한 결정을 내릴 수 있게 됩니다. 예를 들어, 게임의 각 라운드에 대한 성공 여부를 평가하고, 그 결과에 따라 게임의 전체 결과나 다음 단계를 결정하는 등의 역할을 수행하게 됩니다.       

간단히 말하면, `ParentSession`은 아키텍처 설계 원칙 중 하나인 '컴포지션 오버 인헤리턴스(Composition over Inheritance)'를 따르는 객체지향 디자인 패턴입니다. 이 패턴은 재사용과 유지보수를 더 용이하게 하는 동시에, 시스템의 각 부분을 더 독립적으로 만들어 주어 시스템 중 하나의 부분에 문제가 생겨도 전체 시스템에 영향을 끼치지 않게 합니다. 

### [ChildSession](Session/ChildSession.cs)

`ChildSession`은 `ParentSession`의 하위 계층에 속하는 세션 객체로, 게임의 작은 단위 동작이나 상황에 대응합니다. `ParentSession`이 통합적이고 큰 영역의 컨텍스트나 상태를 관리하는 반면, `ChildSession`은 그보다 작고 구체적인 개념을 나타내는 상황에 초점을 맞춥니다.

예를 들어, `ParentSession`이 게임의 전체 라운드를 관리한다면, `ChildSession`은 게임 플레이 상황 중에 발생하는 각각의 이벤트, 예를 들어 플레이어의 특정 행동, 아이템의 사용, 몬스터와의 교전 등을 나타낼 수 있습니다.    

 `ChildSession`의 주요 역할 및 특징은 다음과 같습니다:    

1. **세션 생명주기 관리**: `ChildSession`은 자신이 담당하는 작은 단위의 세션 생명주기를 관리합니다. 이것은 일반적으로           세션의 시작, 진행, 종료 등의 단계를 포함합니다. 각각의 단계에서 필요한 로직을 수행하거나 이벤트를 발생시킵니다.        
2. **세부 게임 상태 관리**: ChildSession`은 더 세부적인 게임 상태를 나타내며 관리합니다. 예를 들어, 플레이어의 특정 행동에서의 결과, 아이템 사용에 따른 게임 상태 변화, 몬스터와의 교전 결과 등을 나타내게 됩니다.
3. **이벤트 리스닝 및 처리**: `ChildSession`은 `ParentSession`으로부터 전달받은 이벤트를 리스닝하고, 어떤 행동을 취할지 결정합니다. 이것은 어떤 특정 행동의 결과, 아이템의 사용, 일정 시간이 경과하는 등의 특정 조건에 반응하는 로직을 수행할 때 유용합니다.        
4. **데이터 제공**: `ChildSession`은 `ParentSession`이 필요로 하는 데이터를 제공합니다. 이는 `ParentSession`이 전반적인 게임 상태를 관리하는데 필요한 정보를 제공하게 됩니다.  

### [RootSession](Session/RootSession.cs)

최상단 세션입니다. 직접적으로 연결된 자식에 대해서만 관리할 권한과 의무를 갖습니다.

이 세션의 구조를 사용하여 World를 구성하고, 하위 구조를 설계할 수 있습니다. 아래는 현재 기본으로 구성한 세션의 구조입니다.

[DefaultWorld](Session/World/DefaultWorld.cs) -> [DefaultMap](Session/World/DefaultMap.cs) -> [DefaultRegion](Session/World/DefaultRegion.cs) -> [DefaultFloor](Session/World/DefaultFloor.cs) -> [DefaultStage](Session/World/DefaultStage.cs)

World는 [GameWorld](Session/World/GameWorld.cs)를 통해 생성될 수 있으며, 각 계층 구조에 맞게 생성되어야합니다. [ParentSessionAttribute](Session/ParentSessionAttribute.cs) 어트리뷰트 선언을 통해 특정 세션 부모를 강제할 수 있습니다. `DefaultStage` 는 상위 세션이 `DefaultFloor` 임을 가정하고 설계하였으므로 이를 적절히 알리기 위해 어트리뷰트를 선언할 수 있습니다.

```C#
[ParentSession(typeof(DefaultFloor), true), Preserve]
public partial class DefaultStage : ChildSession<DefaultStage.SessionData>, IStageProvider,
    IConnector<IActorProvider>
```

------

## Model

 `Model`은 Excel sheet를 기반으로 설계하여 기획자의 요구사항을 맞추고, 게임내 다양한 요소들에 대해 직간접적인 조작이 가능하도록 설계되었습니다. 또한 최소한의 가공으로 원래의 데이터를 보존하도록하고, `Controller`에게 필요한 정보들을 주는 것을 목표로 합니다. [StatValues](Model/Stat/StatValues.cs), [ConditionQuery](Model/ConditionQuery.cs), [StatType](Model/Stat/StatType.cs), [Method](Model/Method.cs) 등은 이러한 작업의 일환으로, 확정된 정보들을 전달할 수 있는 역할을 합니다.

| Cancellation    |       |             |                | AbnormalChain |      |      |
| --------------- | ----- | ----------- | -------------- | ------------- | ---- | ---- |
| Condition       | Value | Probability | ClearAllStacks | 1             | 2    | 3    |
| OnAbnormalAdded |       | 100         | FALSE          | S0000         |      |      |

위는 이상현상을 관리하는 데이터 시트 중 일부입니다. `AbnormalChain` 은 기획 필요에 의해 3개를 초과하는 값이 필요하더라도 프로그래머의 개입없이 행을 추가하기만 하면 자동으로 확장되는 구조를 갖고 있습니다. 

| Duration  |      | TimeCondition |      |        |
| --------- | ---- | ------------- | ---- | ------ |
| DelayTime | Time | 1             | 2    | 3      |
|           | 10   | Always        | AND  | Always |

지속시간에 대한 제어도 프로그래머의 별도 개입없이 가능하도록 설계되었습니다. `TimeCondition` 또한 동적으로 배열이 할당될 수 있으며, `OR`, `AND` 게이트 사용으로 게임의 필수 컨디션들을 연산할 수 있습니다.

| TimeCondition |      |          |
| ------------- | ---- | -------- |
| 1             | 2    | 3        |
| HasPassive    | AND  | IsInHand |

다음과 같이 아무 패시브를 갖고있고, 손에 들고있다면 지속시간이 차감될 수 있다는 의미로 만들어질 수 있습니다.

이러한 [Condition](Model/Condition.cs)(이하 조건)들은 시스템 내에서 시간 단위로 관리되며. 조건은 게임의 볼륨에 따라 매우 많이 증가할 수 있어(+64개 그 이상) Bitmask 형태를 고려하지 않았으므로, 추가적인 그룹 구조가 필요하였습니다. 이를 해결하기 위해 [ConditionQuery](Model/ConditionQuery.cs)를 설계하여 발생한 이벤트들을 최대 64개 단위로 묶어 `Controller`가 이를 확인할 수 있습니다.

조건들은 순차 정수로 정의되어 비트 마스킹이 불가능하기 때문에 여러 조건들을 한번에 검사하는 것은 이것만으로는 불가능합니다. 이를 일일이 검사하는 것은 무의미한 시간낭비이고, 연산 낭비에 속하기 때문에 비트 마스킹을 가능하도록 하는 쿼리를 여러 연산자를 오버로딩하여 알맞게 사용할 수 있도록 설계하였습니다.

```C#
public static implicit operator ConditionQuery(Condition c) => new ConditionQuery((short)c, 1);
```

단일 조건을 먼저 쿼리로 변환할 수 있도록하고,

```c#
public static ConditionQuery operator |(ConditionQuery x, ConditionQuery y)
{
    short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
    if (64 <= math.abs(o - x.m_Offset) ||
        64 <= math.abs(o - y.m_Offset))
        throw new InvalidOperationException($"exceed query");
    if (0 < y.m_Filter && x.m_Offset + 63 < (int)y.Last)
        throw new InvalidOperationException($"exceed query");

    long xf = x.m_Filter << math.abs(o - x.m_Offset),
        yf  = y.m_Filter << math.abs(o - y.m_Offset);

    return new ConditionQuery(o, xf | yf);
}
```

여러 조건들을 병합할 수 있도록 | 연산자를 오버로딩합니다.

```c#
public static ConditionQuery operator &(ConditionQuery x, ConditionQuery y)
{
    if (64 <= math.abs(x.m_Offset - y.m_Offset)) return default;

    short o = x.m_Offset < y.m_Offset ? x.m_Offset : y.m_Offset;
    if (64 <= math.abs(o - x.m_Offset) ||
        64 <= math.abs(o - y.m_Offset))
        throw new InvalidOperationException($"exceed query");

    int yo = math.abs(o - y.m_Offset);
    long xf = x.m_Filter << math.abs(o - x.m_Offset),
        yf  = y.m_Filter << yo;
    yo -= 64;
    while (0 < yo)
    {
        yf &= ~(1L << yo--);
    }

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

스탯은 최대 64개를 기획자가 개발할 수 있도록 설계되었으며, 이때 시스템에서 직접 개입하는 스탯에 대해서는 [StatType](Model/Stat/StatType.cs)으로 관리하며, 중요 시스템에서 사용되는 스탯 값이 아닌 스탯 값들은 `Unknown` 스탯 값으로 구분되어 long 값을 통해 시스템에서 동적으로 추론하여 스탯을 참조하고 할당합니다.

중요 스탯 이외는 프로그래머가 인지하지 못하는 값으로 시스템에 존재하므로, 연산을 위해 다양한 Operator 를 추가하였습니다. [StatValues](Model/Stat/StatValues.cs)는 존재하는 스탯만을 위한 배열을 생성하는데 (예를 들어 `HP | MP` = 길이 2의 배열) 이를 각 코드에서 프로그래머가 제어하는 것은 대단히 위험하고 복잡한 작업이 될 것입니다.

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

[Provider](Provider/Provider.cs)는 값을 제공받고, 연결된 모든 [IConnector](Provider/IConnector.cs)에게 값을 제공하는 역할을 합니다. `Controller`로부터 데이터를 받으면 즉시 연결된 모든 [IConnector](Provider/IConnector.cs)(대부분 `Controller`)에게 값을 전달하는 방식과, 필요에 의해 값을 제공받을 수 있는 `Lazy`(지연 제공)로 구분됩니다.

`Provider` 구조체는 서비스 로케이터 패턴을 구현합니다. [IProvider](Provider/IProvider.cs) 인터페이스를 구현하는 객체를 등록하고, 이들을 관리하게 됩니다. 관리되는 `IProvider`들은 `Type`을 통해 식별되고, `Register`, `Unregister`, `GetAsync`, `ConnectAsync`, `Connect`, `Disconnect` 등의 메소드를 통해 접근하고 조작할 수 있습니다. 

1. **코드 간 결합도를 낮춤**: 서비스를 사용하는 클라이언트는 서비스 로케이터를 통해 서비스를 얻기 때문에, 원하는 서비스의 실제           구현에 대해 알 필요가 없습니다. 이를 통해 코드 간 결합도를 낮춤으로써 유지 보수성과 확장성을 높일 수 있습니다.        
2. **서비스 교체의 용이성**: 서비스 로케이터에 서비스를 등록할 때 어떠한 인터페이스를 구현하는 지에 대한 정보만 있으면 되므로,           특정 서비스의 구현을 교체하거나 변경하는 것이 용이합니다.        
3. **다양한 서비스 라이프사이클 관리**: 서비스 로케이터는 서비스의 라이프사이클도 관리할 수 있습니다. 이를 통해 싱글턴, 프로토타입 등 다양한 라이프사이클을 가진 서비스들을 동일한 방식으로 관리할 수 있습니다.        

`IProvider` 인터페이스는 이벤트 객체를 직접적으로 알고있거나, 조건에 대해 제공할 의무가 있는 `Controller`를 위해 설계되었습니다. [DefaultStage](Session/World/DefaultStage.cs) 객체는 스테이지에 대한 모든 액터에 대해 제공할 의무가 있는 설계상 가장 하위 `Session`입니다. 예를 들어, [GameConfigSheet](Model/GameConfigSheet.cs)에서 정의된 조건에 맞는 메서드를 생성하여 반환합니다.

```C#
GameMethodImplDelegate IGameMethodProvider.Resolve(Model.GameMethod method)
{
  if (method == Model.GameMethod.Destroy)
  {
    return GameMethod_Destroy;
  }

  if (method == Model.GameMethod.ExecuteBehaviorTree)
  {
    return GameMethod_ExecuteBehaviorTree;
  }

  throw new NotImplementedException();
}

private async UniTask GameMethod_Destroy(IEventTarget e, IReadOnlyList<string> parameters)
{
  if (e is not IActor x) return;

  // m_DestroyProcessing = true;
  using (var trigger = ConditionTrigger.Push(x, nameof(Model.GameMethod)))
  {
    await trigger.Execute(Model.Condition.OnBattleEnd, null);
    await trigger.Execute(Model.Condition.OnActorDead, null);
  }

  var field = x.ConditionResolver[Model.Condition.IsPlayerActor](null) ? m_PlayerField : m_EnemyField;
  int index = field.FindIndex(e => e.owner == x);
  if (index < 0)
  {
    $"{index} not found in field {x.ConditionResolver[Model.Condition.IsPlayerActor](null)}".ToLogError();
    return;
  }

  RuntimeActor actor = field[index];

  $"Actor {actor.owner.DisplayName} is dead {actor.owner.Stats[StatType.HP]}".ToLog();

  Assert.IsFalse(Disposed);
  await Delete(field, actor);
}
```

이 메소드를 상속받은 `DefaultStage` 는 [LocalProviderAttribute](Provider/LocalProviderAttribute.cs) 를 상속받은 [IGameMethodProvider](Provider/IGameMethodProvider.cs) 를 구현하고 있습니다. 

```c#
[LocalProvider]
public interface IGameMethodProvider : IProvider
{
    GameMethodImplDelegate Resolve(Model.GameMethod method);
}
```

로컬로 마킹된 `IProvider`는 전역 `Provider`를 통해 공급될 수 없고, 상위 객체로부터 의존성을 주입받아야합니다. `DefaultStage` 는 `IConnector<IActorProvider>` 인터페이스를 상속받아 상위 세션으로부터 [IActorProvider](Session/Provider/IActorProvider.cs)에 대한 의존성 주입을 하고 있습니다. 여기서 상위 세션인 `DefaultWorld` 는 `IActorProvider` 를 구현하고 등록하고 있습니다. 

```c#
public partial class DefaultWorld : RootSession, IWorldSession,
        IConnector<IStateConditionProvider>,
        IConnector<IGameConfigProvider>,
        IActorProvider
```

상위 세션이 `IProvider`를 가지고 있다면 하위 세션이 생성될 때 생성된 세션에 대해 해당 `IProvider`에 대한 의존성을 검사하고 주입합니다. 또한, 상위 세션은 하위 세션에 대해 `IProvider`에 대해 검사하고 반환받을 수 있습니다.

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

이 시스템으로 관리되는 모든 이벤트 객체는 `IEventTarget` 을 상속받습니다. 이는 의존성 역전 원칙(Dependency Inversion Principle, DIP)에 따른 것으로, 각 인터페이스를 사용하는 클래스는 구체적인 구현물이 아닌 추상화에 의존하며, 각 구현 클래스는 이 인터페이스를 구현함으로써 상세 사항이 추상화를 향해 의존하게 됩니다. 이로 인해 각 클래스와 모듈 사이의 결합도는 낮아지고, 재사용성과 테스트 용이성은 증가하게 됩니다. 또한 이는 시스템 구성 요소 간의 유연한 상호 작용을 가능하게 하여 변화에 더 잘 적응하고 확장할 수 있는 소프트웨어 디자인을 가능하게 합니다.    

`IEventTarget`은 공통 속성으로 `DisplayName`와 `Disposed`, [Owner](Provider/Owner.cs)를 갖고있고, `Owner`를 통해 각 클라이언트를 구분하거나 AI 를 구분지을 수 있습니다.

------

## Controller

`Controller`는 시스템 내에서 최종적인 데이터 가공이 이루어지고, 실제 `View`에게 공급하는 역할을 합니다. 사용자에 의해 정의된 모든 이벤트 주체는 [IEventTarget](Provider/IEventTarget.cs)을 통해 모든 조건들에 대해 검사할 수 있으며, 사용자에 의해 정의된 플레이어 객체 [IActor](Controller/Actor/IActor.cs)(이하 액터) 만을 위한 조건도 존재합니다. 이는 조건을 제공하는 `Controller`에서 `Provider`에게 해당 조건을 제공하고, [SkillController](Controller/Skill/SkillController.cs), [AbnormalController](Controller/Abnormal/AbnormalController.cs), [PassiveController](Controller/Passive/PassiveController.cs)등에서 조건들에 대해 [ConditionTrigger](Controller/Condition/ConditionTrigger.cs)로 공급하게 합니다.

### ConditionTrigger

[ConditionTrigger](Controller/Condition/ConditionTrigger.cs)는 Flyweight 디자인 패턴으로 설계된 게임내 이벤트들에 대한 모든 조건에 대해 발생하였음을 알리는 Event Broadcaster 입니다. 

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

공급받은 `value`값은 시트에서 정의된 `value`값이거나, `OnHit`(이 경우, `value`값은 히트된 데미지를 의미)와 같이 시스템에서 발생하는 이벤트의 값 입니다. 

소멸자 패턴을 활용하여 이벤트 객체에 대한 전체 `Scope`(이하 스코프)를 생성하고, 해당 스코프 내에서는 그 이벤트 객체에 대한 조건임을 명시합니다.

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

해당 액터에 대한 조건임을 명시하고, 하위 모든 트리거에 대한 이벤트의 시작점이 해당 액터임을 명시합니다. 이는 액터의 행동 시간 중 다른 액터의 행동 또한 발생할 수 있기 때문인데, 예를 들어 스킬을 사용한 후, 피격당한 액터가 다시 반격을 하거나, 또는 그 이전에 `OnHit` 조건을 발생시킬수 있기 떄문에 모든 이벤트에 대해 소유자를 명시하여야합니다.

이렇게 발생한 조건들에 대해, 그 조건을 소유(즉, 이 이벤트 객체의 시간내에서 조건이 발생한 적 있는지)를 검사할 수 있고, 그 외 필요한 모든 조건에 대해 검사할 수 있는 [ConditionResolver](Controller/Condition/ConditionResolver.cs)를 설계하였습니다.

### ConditionResolver

`Provider` 구조체는 전역에 대해 제공할 의무가 있다면, [ConditionResolver](Controller/Condition/ConditionResolver.cs)는 지역에 대해서만 제공할 의무가 있는 객체입니다. 즉, 모든 이벤트 객체는 [ConditionResolver](Controller/Condition/ConditionResolver.cs)를 소유할 수 있으며, 자신에 대한 값을 제공하는 [IProvider](Provider/IProvider.cs)들을 연결하여 한번에 값을 해결할 수 있습니다.

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

이렇게 연결되면 해당 조건에 대해 해결할 의무는 온전히 Provider, 즉 여기서는 [IStatConditionProvider](Provider/IStatConditionProvider.cs)에게 이관됩니다. 이 인터페이스를 상속받는 [StatProvider](Provider/StatProvider.cs)는 시트에서 문자열로 입력받은 값에 대해 파싱하고, 검증하여 반환하도록 보장합니다.

연결된 스탯은 델리게이트 생성자를 통해 간접 참조를 수행할 수 있습니다.

```C#
public delegate float StatValueGetterDelegate(in IReadOnlyStatValues stat);
public delegate void StatValueSetterDelegate(ref StatValues stat, float value);

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
    d              = (ref StatValues x, float value) => x.SetValue(t, value);
    s_CachedSetter[t] = d;
  }

  return d;
}
```

스탯 타입(프로그램에 정의되지 않은 값도 가능)으로 해당 스탯 타입으로 연결하는 델리게이트를 얻을 수 있고, 이것을 통해 각 `Controller`의 기능 수행 부분은 스탯이 무엇인지 알지못해도 알맞는 스탯에 대해 연산을 수행할 수 있습니다.

```C#
void IStatModifier.UpdateValues(in IReadOnlyStatValues originalStats, ref StatValues stats)
{
  foreach (Value e in m_Values.OrderBy(ValueMethodOrderComparer.Selector, ValueMethodOrderComparer.Static))
  {
    int length = e.updateCount;
    for (int i = 0; i < length; i++)
    {
      e.abnormal.setter(ref stats, e.abnormal.method(
        e.abnormal.getter(stats),
        e.abnormal.value
      ));
    }
  }
  m_IsDirty = false;
}
```

이것은 개방-폐쇄 원칙(Open-Closed Principle, OCP)을 적용한 것으로, 메서드의 모든 부분이 추상화에 의존하고 있기 때문입니다. 실제로 참조하고 있는 `Value`의 `setter`와 `getter`는 `StatValues`에서 캐시된 델리게이트를 전달받고 있습니다.

```C#
public readonly StatValueGetterDelegate getter;
public readonly StatValueSetterDelegate setter;

public RuntimeAbnormal(AbnormalSheet.Row d)
{
  // ....
  getter     = StatValues.GetGetMethod(targetStat);
  setter     = StatValues.GetSetMethod(targetStat);
  // ....
}
```

이러한 방식은 주어진 상황에 따라 동적으로 필요한 메서드를 선택할 수 있도록 합니다. `StatValues`에서는 `GetGetMethod` 및 `GetSetMethod` 정적 메서드를 제공하여 특정 `StatType`에 대한 `getter`와 `setter`를 동적으로 가져옵니다. 이것은 핵심 `StatType`에 따라 스탯 값을 가져오거나 설정하는 방법을 선택할 수 있게 해줍니다. 이 접근 방식은 코드의 읽기 어려움을 약간 증가시킬 수 있지만, 이를 통해 동적 행동과 유연성을 증가시킬 수 있습니다,
