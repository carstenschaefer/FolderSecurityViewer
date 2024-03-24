using PostSharp.Extensibility;
using PostSharp.Patterns.Diagnostics;

[assembly: Log(AttributePriority = 1, AttributeTargetMemberAttributes = MulticastAttributes.Protected | MulticastAttributes.Internal | MulticastAttributes.Public | MulticastAttributes.Private)]
[assembly: Log(AttributePriority = 2, AttributeExclude = true, AttributeTargetMembers = "regex:(get|set)_.*")]