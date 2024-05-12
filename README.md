# Vvr.System

*Part of **Vvr Framework**®  
Copyright 2022 Syadeu. All rights reserved* 

------

이 시스템은 MVPC, Model, View, Provider(Presenter), Controller 으로 설계되었습니다. 

Model은 게임에서의 가장 기초적인 데이터를 다루고 있습니다. 예를 들어, AbnormalSheet와 같이 기획에서 구성된 데이터를 기초 가공하여 저장합니다.

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

이러한 Condition(이하 조건)들은 시스템 내에서 시간 단위로 관리되며. 조건은 게임의 볼륨에 따라 매우 많이 증가할 수 있어(+64개 그 이상) Bitmask 형태를 고려하지 않았으므로, 추가적인 그룹 구조가 필요하였습니다. 이를 해결하기 위해 ConditionQuery 를 설계하여 발생한 이벤트들을 최대 64개 단위로 묶어 Controller가 이를 확인할 수 있습니다. 

| Stats |      |      |      |      |
| ----- | ---- | ---- | ---- | ---- |
| SPD   | ATT  | DEF  | ARM  | HP   |
| 10    | 20   | 5    | 2    | 300  |

Stat(이하 스탯)또한 기획 필요로 인해 새로운 스탯이 추가되어도 이를 별도 스탯 데이터 시트에 추가하고, 해당 ID 를 입력하면 게임 내에서 동적으로 해결되도록 설계하였습니다. 이를 가능하게 하기 위해 시트에서 값을 가져오는 UnresolvedStatValues 와 스탯 데이터 시트를 통해 해결된 값들을 담는 StatValues 로 설계되었습니다.

스탯은 최대 64개를 기획자가 개발할 수 있도록 설계되었으며, 중요 시스템에서 사용되는 스탯 값이 아닌 스탯 값들은 Unknown 스탯 값으로 구분되어 long 값을 통해 시스템에서 동적으로 스탯을 참조하고 할당합니다.

이 시트의 구조는 https://docs.google.com/spreadsheets/d/1210EHg1DeiuPKkWcynhwGYKuEg2DfxmbKICNM4rGw8c/edit?usp=sharing 에서 확인할 수 있습니다.

------

## Provider

Provider는 값을 제공받고, 연결된 모든 Connector 에게 값을 보장하는 역할을 합니다. Model로부터 데이터를 받으면 즉시 연결된 모든 Connector(대부분 Controller)에게 값을 전달하는 방식과 필요에 의해 값을 보장받을 수 있는 Lazy 로 구분됩니다.

------

## Controller

Controller는 시스템 내에서 최종적인 데이터 가공이 이루어지고, 실제 View 에게 공급하는 역할을 합니다. 사용자에 의해 정의된 모든 이벤트 주체는 IEventTarget 을 통해 모든 조건들에 대해 검사할 수 있으며, 사용자에 의해 정의된 플레이어 객체 IActor(이하 액터) 만을 위한 조건도 존재합니다. 이는 조건을 제공하는 Controller에서 Provider 에게 해당 조건을 제공하고, SkillContainer, AbnormalController, PassiveController등에서 조건들에 대해 ConditionTrigger 로 공급하게 합니다.

ConditionTrigger는 게임내 이벤트들에 대한 모든 조건에 대해 발생하였음을 알리는 Event Broadcaster 입니다. 

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

이렇게 발생한 조건들에 대해, 그 조건을 소유(즉, 이 이벤트 객체의 시간내에서 조건이 발생한 적 있는지)를 검사할 수 있고, 그 외 필요한 모든 조건에 대해 검사할 수 있는 ConditionResolver 를 설계하였습니다.

Provider 구조체는 전역에 대해 제공할 의무가 있다면, ConditionResolver 는 지역에 대해서만 제공할 의무가 있는 객체입니다. 즉, 모든 이벤트 객체는 ConditionResolver 를 소유할 수 있으며, 자신에 대한 값을 제공하는 IProvider들을 연결하여 한번에 값을 해결할 수 있습니다.

만약, 소유한 이벤트 객체가 액터이고, 액터임으로 스탯을 보유한다고 가정할 때, 이 ConditionResolver 를 통해 특정 스탯에 대해 값을 해결 할 수 있습니다.

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