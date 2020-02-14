# Unity 3D Reinforcement Learning Based Self Learning Airplane Agents with Shooting Mechanism

#### Setup and Installation
 - Downlaod and Install ML-Agents-13
 
  `https://github.com/Unity-Technologies/ml-agents/blob/latest_release/docs/Readme.md`
  
 - Navigate to the cloned ML-Agents folder and open cmd terminal
 - Use the following commands to setup environement. Make sure to maintain the order of excecution
 
 `cd ml-agents-envs ` <br/>
`pip3 install -e ./ ` <br/>
`cd .. ` <br/>
`cd ml-agents ` <br/>
`pip3 install -e ./`

 - Download and Clone the Airplane project and keep in the folder where ML-Agents folder is placed.
 - Open Unity and import the project.
 
### Training the environment
#### Open a command or terminal window.

- Navigate to the folder where you cloned the ML-Agents toolkit repository. Note: If you followed the default installation, then you should be able to run mlagents-learn from any directory.

- Run `mlagents-learn <trainer-config-path> --run-id=<run-identifier> --train` where:

  - `<trainer-config-path>` is the relative or absolute filepath of the trainer configuration. The defaults used by example environments included in MLAgentsSDK can be found in `config/trainer_config.yaml`.
`<run-identifier>` is a string used to separate the results of different training runs
`--train` tells mlagents-learn to run a training session (rather than inference)
If you cloned the ML-Agents repo, then you can simply run

`mlagents-learn config/trainer_config.yaml --run-id=firstRun --train`

When the message "Start training by pressing the Play button in the Unity Editor" is displayed on the screen, you can press the ▶️ button in Unity to start training in the Editor.
