# statemachineworkflow
A project to run workflows as DAGs with long running steps.

## The motivation of this project is not to provide yet another workflow engine capable of running on a cloud native host but to provide a framework capable of  
replaying a given DAG workflow as many times as needed feeding it a representation of the workflow state making it de facto a state machine workflow.

## Key Tenants and Ideas:
- The definition of what a step does is completely divorced from the execution of the said step in the workflow execution runtime. 
This is achieved by using an asynchronous message based communcation between the workflow runtime and the host processing events emitted by the workflow while being executed.
- The state book keeping responsibility falls onto the implementer but the interface defining what the workflow state is is mandated and defined by the framework.

## Use Cases:
- This framework is suited when you need to run a long running workflow where each step completion is driven by an external system or application.
- This framework show cases long running workflows running in an Azure Logic App infrastructure and is:
  - scalable from running as a Logic App on the Azure Functions runtime,
  - cost effective as each execution is time bound,
  - reliable as it is meant to be replayable by design
  - resilient as you control the configuration of the Azure App host (multi-region, multi-zone ETC...)

## Limitations:
- The base/default implementation targets Azure Logic Apps and can only be run on the Azure platform as the representation of the workflow is authored using the Azure Logic App DSL language.
- This implementation has limitation around loops and remains first and foremost a DAG workflow implementation.
