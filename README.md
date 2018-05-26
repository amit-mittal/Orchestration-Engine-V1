# Orchestration-Engine-V1
Orchestration Engine Prototype V1 to take care of the lifecyle of the tenant (any entity) job from front-end to actually take the action and propagate status back to the front-end. It has been written on top of the service fabric.

## Components
- FrontEnd: Stateless
- RuntimeStore: Stateful (Stores the state of jobs)
- InferenceEngine: Stateless (Decides which type of action to take on tenant as per its state)
- Orchestrator: Stateful (Can be made stateless as well but, after the inference, it orchestrates with the actor)
- TenantUpdateActor: Stateful Actor per job/tenant (Takes the final action on tenant)