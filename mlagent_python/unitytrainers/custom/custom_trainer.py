# # Unity ML Agents

import logging
import os

import numpy as np
import tensorflow as tf

from unityagents import AllBrainInfo
from unitytrainers.buffer import Buffer
from unitytrainers.custom.custom_models import CustomModel
from unitytrainers.trainer import UnityTrainerException, Trainer

logger = logging.getLogger("unityagents")


class CustomTrainer(Trainer):
    """The CustomPPOTrainer is an custom changing version of PPOTrainer."""

    def __init__(self, sess, env, brain_name, trainer_parameters, training, seed):
        """
        Responsible for collecting experiences and training PPO model.
        :param sess: Tensorflow session.
        :param env: The UnityEnvironment.
        :param  trainer_parameters: The parameters for the trainer (dictionary).
        :param training: Whether the trainer is set for training.
        """
        self.param_keys = ['batch_size', 'beta', 'buffer_size', 'epsilon', 'gamma', 'hidden_units', 'lambd',
                           'learning_rate',
                           'max_steps', 'normalize', 'num_epoch', 'num_layers', 'time_horizon', 'sequence_length',
                           'summary_freq',
                           'use_recurrent', 'graph_scope', 'summary_path', 'memory_size']

        for k in self.param_keys:
            if k not in trainer_parameters:
                raise UnityTrainerException("The hyperparameter {0} could not be found for the PPO trainer of "
                                            "brain {1}.".format(k, brain_name))

        super(CustomTrainer, self).__init__(sess, env, brain_name, trainer_parameters, training)

        self.use_recurrent = trainer_parameters["use_recurrent"]
        self.sequence_length = 1
        self.m_size = None
        if self.use_recurrent:
            self.m_size = trainer_parameters["memory_size"]
            self.sequence_length = trainer_parameters["sequence_length"]
        if self.use_recurrent:
            if self.m_size == 0:
                raise UnityTrainerException("The memory size for brain {0} is 0 even though the trainer uses recurrent."
                                            .format(brain_name))
            elif self.m_size % 4 != 0:
                raise UnityTrainerException("The memory size for brain {0} is {1} but it must be divisible by 4."
                                            .format(brain_name, self.m_size))

        self.variable_scope = trainer_parameters['graph_scope']
        with tf.variable_scope(self.variable_scope):
            tf.set_random_seed(seed)
            self.model = CustomModel(
                h_size=int(trainer_parameters['hidden_units']),
                lr=float(trainer_parameters['learning_rate']),
                n_layers=int(trainer_parameters['num_layers']),
                m_size=self.m_size,
                normalize=False,
                use_recurrent=trainer_parameters['use_recurrent'],
                brain=self.brain)

        stats = {'cumulative_reward': [], 'episode_length': [], 'losses': []}
        self.stats = stats

        self.training_buffer = Buffer()
        self.cumulative_rewards = {}
        self.episode_steps = {}
        self.is_continuous_action = (env.brains[brain_name].vector_action_space_type == "continuous")
        self.is_continuous_observation = (env.brains[brain_name].vector_observation_space_type == "continuous")
        self.use_observations = (env.brains[brain_name].number_visual_observations > 0)
        self.use_states = (env.brains[brain_name].vector_observation_space_size > 0)
        self.summary_path = trainer_parameters['summary_path']
        if not os.path.exists(self.summary_path):
            os.makedirs(self.summary_path)

        self.summary_writer = tf.summary.FileWriter(self.summary_path)

    def __str__(self):
        return '''Hypermarameters for the Custom Trainer of brain {0}: \n{1}'''.format(
            self.brain_name, '\n'.join(['\t{0}:\t{1}'.format(x, self.trainer_parameters[x]) for x in self.param_keys]))

    @property
    def parameters(self):
        """
        Returns the trainer parameters of the trainer.
        """
        return self.trainer_parameters

    @property
    def graph_scope(self):
        """
        Returns the graph scope of the trainer.
        """
        return self.variable_scope

    @property
    def get_max_steps(self):
        """
        Returns the maximum number of steps. Is used to know when the trainer should be stopped.
        :return: The maximum number of steps of the trainer
        """
        return float(self.trainer_parameters['max_steps'])

    @property
    def get_step(self):
        """
        Returns the number of steps the trainer has performed
        :return: the step count of the trainer
        """
        return self.sess.run(self.model.global_step)

    @property
    def get_last_reward(self):
        """
        Returns the last reward the trainer has had
        :return: the new last reward
        """
        if len(self.stats['cumulative_reward']) > 0:
            return np.mean(self.stats['cumulative_reward'])
        else:
            return 0

    def increment_step(self):
        """
        Increment the step count of the trainer
        """
        self.sess.run(self.model.increment_step)

    def update_last_reward(self):
        """
        Updates the last reward
        """
        return

    def take_action(self, all_brain_info: AllBrainInfo):
        """
        Decides actions given state/observation information, and takes them in environment.
        :param all_brain_info: A dictionary of brain names and BrainInfo from environment.
        :return: a tuple containing action, memories, values and an object
        to be passed to add experiences
        """
        if len(all_brain_info[self.brain_name].agents) == 0:
            return [], [], [], None

        agent_brain = all_brain_info[self.brain_name]
        feed_dict = {self.model.dropout_rate: 1.0, self.model.sequence_length: 1}
        run_list = [self.model.sample_action]
        if self.use_observations:
            for i, _ in enumerate(agent_brain.visual_observations):
                feed_dict[self.model.visual_in[i]] = agent_brain.visual_observations[i]
        if self.use_states:
            feed_dict[self.model.vector_in] = agent_brain.vector_observations
        if self.use_recurrent:
            if agent_brain.memories.shape[1] == 0:
                agent_brain.memories = np.zeros((len(agent_brain.agents), self.m_size))
            feed_dict[self.model.memory_in] = agent_brain.memories
            run_list += [self.model.memory_out]
        if self.use_recurrent:
            agent_action, memories = self.sess.run(run_list, feed_dict)
            return agent_action, memories, None, None
        else:
            agent_action = self.sess.run(run_list, feed_dict)
        return agent_action, None, None, None

    def add_experiences(self, curr_all_info: AllBrainInfo, next_all_info: AllBrainInfo, take_action_outputs):
        """
        Adds experiences to each agent's experience history.
        :param curr_all_info: Dictionary of all current brains and corresponding BrainInfo.
        :param next_all_info: Dictionary of all current brains and corresponding BrainInfo.
        :param take_action_outputs: The outputs of the take action method.
        """
        curr_info = curr_all_info[self.brain_name]
        next_info = next_all_info[self.brain_name]

        for agent_id in curr_info.agents:
            self.training_buffer[agent_id].last_brain_info = curr_info
            self.training_buffer[agent_id].last_take_action_outputs = take_action_outputs

        for agent_id in next_info.agents:
            stored_info = self.training_buffer[agent_id].last_brain_info
            stored_take_action_outputs = self.training_buffer[agent_id].last_take_action_outputs
            if stored_info is None:
                continue
            else:
                idx = stored_info.agents.index(agent_id)
                next_idx = next_info.agents.index(agent_id)
                if not stored_info.local_done[idx]:
                    if self.use_states:
                        self.training_buffer[agent_id]['states'].append(stored_info.vector_observations[idx])

                    self.training_buffer[agent_id]['actions'].append(next_info.rewards[next_idx])

                    if agent_id not in self.cumulative_rewards:
                        self.cumulative_rewards[agent_id] = 0
                    self.cumulative_rewards[agent_id] += next_info.rewards[next_idx]
                    if agent_id not in self.episode_steps:
                        self.episode_steps[agent_id] = 0
                    self.episode_steps[agent_id] += 1

    def process_experiences(self, current_info: AllBrainInfo, new_info: AllBrainInfo):
        """
        Checks agent histories for processing condition, and processes them as necessary.
        Processing involves calculating value and advantage targets for model updating step.
        :param current_info: Dictionary of all current brains and corresponding BrainInfo.
        :param new_info: Dictionary of all next brains and corresponding BrainInfo.
        """

        info = new_info[self.brain_name]
        for l in range(len(info.agents)):
            agent_actions = self.training_buffer[info.agents[l]]['actions']
            if ((info.local_done[l] or len(agent_actions) > self.trainer_parameters['time_horizon'])
                and len(agent_actions) > 0):
                agent_id = info.agents[l]
                
                self.training_buffer.append_update_buffer(agent_id,
                                                          batch_size=None, training_length=self.sequence_length)

                self.training_buffer[agent_id].reset_agent()
                if info.local_done[l]:
                    self.stats['cumulative_reward'].append(self.cumulative_rewards[agent_id])
                    self.stats['episode_length'].append(self.episode_steps[agent_id])
                    self.cumulative_rewards[agent_id] = 0
                    self.episode_steps[agent_id] = 0

    def end_episode(self):
        """
        A signal that the Episode has ended. The buffer must be reset. 
        Get only called when the academy resets.
        """
        self.training_buffer.reset_all()
        for agent_id in self.cumulative_rewards:
            self.cumulative_rewards[agent_id] = 0
        for agent_id in self.episode_steps:
            self.episode_steps[agent_id] = 0

    def is_ready_update(self):
        """
        Returns whether or not the trainer has enough elements to run update model
        :return: A boolean corresponding to whether or not update_model() can be run
        """
        return len(self.training_buffer.update_buffer['actions']) > \
               max(int(self.trainer_parameters['buffer_size'] / self.sequence_length), 1)

    def update_model(self):
        """
        Uses training_buffer to update model.
        """
        num_epoch = self.trainer_parameters['num_epoch']
        n_sequences = max(int(self.trainer_parameters['batch_size'] / self.sequence_length), 1)
        batch_losses = []

        for k in range(num_epoch):
            self.training_buffer.update_buffer.shuffle()
            for l in range(len(self.training_buffer.update_buffer['actions']) // n_sequences):
                start = l * n_sequences
                end = (l + 1) * n_sequences
                _buffer = self.training_buffer.update_buffer

                batch_actions = np.array(_buffer['actions'][start:end])

                feed_dict = {self.model.dropout_rate: 0.5,
                             self.model.batch_size: n_sequences,
                             self.model.sequence_length: self.sequence_length}

                if self.is_continuous_action:
                    feed_dict[self.model.true_action] = batch_actions.reshape([-1, self.brain.vector_action_space_size])
                else:
                    feed_dict[self.model.true_action] = batch_actions.reshape([-1])

                if self.use_states:
                    if self.is_continuous_observation:
                        feed_dict[self.model.vector_in] = np.array(
                            _buffer['states'][start:end]).reshape(
                            [-1, self.brain.vector_observation_space_size * self.brain.num_stacked_vector_observations])
                    else:
                        feed_dict[self.model.vector_in] = np.array(
                            _buffer['states'][start:end]).reshape([-1, self.brain.num_stacked_vector_observations])
                
                loss, _ = self.sess.run([self.model.loss, self.model.update], feed_dict=feed_dict)
                batch_losses.append(loss)
        if len(batch_losses) > 0:
            self.stats['losses'].append(np.mean(batch_losses))
        else:
            self.stats['losses'].append(0)
        self.training_buffer.reset_update_buffer()

    def write_summary(self, lesson_number):
        """
        Saves training statistics to Tensorboard.
        :param lesson_number: The lesson the trainer is at.
        """
        if (self.get_step % self.trainer_parameters['summary_freq'] == 0 and self.get_step != 0 and
                self.is_training and self.get_step <= self.get_max_steps):
            steps = self.get_step
            if len(self.stats['cumulative_reward']) > 0:
                mean_reward = np.mean(self.stats['cumulative_reward'])
                logger.info(" {}: Step: {}. Mean Reward: {:0.3f}. Std of Reward: {:0.3f}."
                            .format(self.brain_name, steps, mean_reward, np.std(self.stats['cumulative_reward'])))
            summary = tf.Summary()
            for key in self.stats:
                if len(self.stats[key]) > 0:
                    stat_mean = float(np.mean(self.stats[key]))
                    summary.value.add(tag='Info/{}'.format(key), simple_value=stat_mean)
                    self.stats[key] = []
            summary.value.add(tag='Info/Lesson', simple_value=lesson_number)
            self.summary_writer.add_summary(summary, steps)
            self.summary_writer.flush()