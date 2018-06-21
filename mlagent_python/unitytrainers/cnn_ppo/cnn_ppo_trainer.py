# # Unity ML Agents

import logging
import os

import numpy as np
import tensorflow as tf

from unityagents import AllBrainInfo
from unitytrainers.buffer import Buffer
from unitytrainers.cnn_ppo.cnn_ppo_models import CNNPPOModel
from unitytrainers.trainer import UnityTrainerException, Trainer
from unitytrainers.ppo.trainer import PPOTrainer

logger = logging.getLogger("unityagents")


class CNNPPOTrainer(PPOTrainer):
    """The PPOTrainer is an implementation of the PPO algorythm."""

    def __init__(self, sess, env, brain_name, trainer_parameters, training, seed):
        """
        Responsible for collecting experiences and training PPO model.
        :param sess: Tensorflow session.
        :param env: The UnityEnvironment.
        :param  trainer_parameters: The parameters for the trainer (dictionary).
        :param training: Whether the trainer is set for training.
        """
        self.param_keys = ['batch_size', 'beta', 'buffer_size', 'epsilon', 'gamma', 'hidden_units', 'lambd',
                           'learning_rate', 'max_steps', 'normalize', 'num_epoch', 'num_layers',
                           'time_horizon', 'sequence_length', 'summary_freq', 'use_recurrent',
                           'graph_scope', 'summary_path', 'memory_size', 'use_curiosity', 'curiosity_strength',
                           'curiosity_enc_size']

        for k in self.param_keys:
            if k not in trainer_parameters:
                raise UnityTrainerException("The hyperparameter {0} could not be found for the PPO trainer of "
                                            "brain {1}.".format(k, brain_name))

        super(PPOTrainer, self).__init__(sess, env, brain_name, trainer_parameters, training)

        self.use_recurrent = trainer_parameters["use_recurrent"]
        self.use_curiosity = bool(trainer_parameters['use_curiosity'])
        self.sequence_length = 1
        self.step = 0
        self.has_updated = False
        self.m_size = None
        if self.use_recurrent:
            self.m_size = trainer_parameters["memory_size"]
            self.sequence_length = trainer_parameters["sequence_length"]
            if self.m_size == 0:
                raise UnityTrainerException("The memory size for brain {0} is 0 even though the trainer uses recurrent."
                                            .format(brain_name))
            elif self.m_size % 4 != 0:
                raise UnityTrainerException("The memory size for brain {0} is {1} but it must be divisible by 4."
                                            .format(brain_name, self.m_size))

        self.variable_scope = trainer_parameters['graph_scope']
        with tf.variable_scope(self.variable_scope):
            tf.set_random_seed(seed)
            self.model = CNNPPOModel(env.brains[brain_name],
                                  lr=float(trainer_parameters['learning_rate']),
                                  h_size=int(trainer_parameters['hidden_units']),
                                  epsilon=float(trainer_parameters['epsilon']),
                                  beta=float(trainer_parameters['beta']),
                                  max_step=float(trainer_parameters['max_steps']),
                                  normalize=trainer_parameters['normalize'],
                                  use_recurrent=trainer_parameters['use_recurrent'],
                                  num_layers=int(trainer_parameters['num_layers']),
                                  m_size=self.m_size,
                                  use_curiosity=bool(trainer_parameters['use_curiosity']),
                                  curiosity_strength=float(trainer_parameters['curiosity_strength']),
                                  curiosity_enc_size=float(trainer_parameters['curiosity_enc_size']))

        stats = {'cumulative_reward': [], 'episode_length': [], 'value_estimate': [],
                 'entropy': [], 'value_loss': [], 'policy_loss': [], 'learning_rate': []}
        if self.use_curiosity:
            stats['forward_loss'] = []
            stats['inverse_loss'] = []
            stats['intrinsic_reward'] = []
            self.intrinsic_rewards = {}
        self.stats = stats

        self.training_buffer = Buffer()
        self.cumulative_rewards = {}
        self.episode_steps = {}
        self.is_continuous_action = (env.brains[brain_name].vector_action_space_type == "continuous")
        self.is_continuous_observation = (env.brains[brain_name].vector_observation_space_type == "continuous")
        self.use_visual_obs = (env.brains[brain_name].number_visual_observations > 0)
        self.use_vector_obs = (env.brains[brain_name].vector_observation_space_size > 0)
        self.summary_path = trainer_parameters['summary_path']
        if not os.path.exists(self.summary_path):
            os.makedirs(self.summary_path)

        self.summary_writer = tf.summary.FileWriter(self.summary_path)

        self.inference_run_list = [self.model.output, self.model.all_probs, self.model.value,
                                   self.model.entropy, self.model.learning_rate]
        if self.is_continuous_action:
            self.inference_run_list.append(self.model.output_pre)
        if self.use_recurrent:
            self.inference_run_list.extend([self.model.memory_out])
        if (self.is_training and self.is_continuous_observation and
                self.use_vector_obs and self.trainer_parameters['normalize']):
            self.inference_run_list.extend([self.model.update_mean, self.model.update_variance])