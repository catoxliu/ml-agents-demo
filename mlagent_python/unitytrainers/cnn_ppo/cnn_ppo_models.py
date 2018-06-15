import logging

import tensorflow as tf
from unitytrainers.models import LearningModel
import tensorflow.contrib.layers as c_layers
from unitytrainers.ppo.models import PPOModel

logger = logging.getLogger("unityagents")


class CNNPPOModel(PPOModel):

    def create_dummy_visual_encoder(self, vector_input, h_size, activation, num_layers):
        """
        Builds a set of customer visual (CNN) encoders.
        Do some trick to reshape the vector input back to image format
        :param image_input: The placeholder for the image input to use.
        :param h_size: Hidden layer size.
        :param activation: What type of activation function to use for layers.
        :param num_layers: number of hidden layers to create.
        :return: List of hidden layer tensors.
        """
        image_input = tf.reshape(vector_input, tf.stack([-1, 40, 16, 1]));
        conv1 = tf.layers.conv2d(image_input, 32, kernel_size=7, padding="same",
                                 activation=tf.nn.relu)
        pool1 = tf.layers.max_pooling2d(inputs=conv1, pool_size=2, strides=2)
        conv2 = tf.layers.conv2d(pool1, 64, kernel_size=5, padding="same",
                                 activation=tf.nn.relu)
        pool2 = tf.layers.max_pooling2d(inputs=conv2, pool_size=2, strides=2)
        hidden = c_layers.flatten(pool2)

        for j in range(num_layers):
            hidden = tf.layers.dense(hidden, h_size, use_bias=False, activation=activation)
        return hidden

    def create_new_obs(self, num_streams, h_size, num_layers):
        brain = self.brain
        s_size = brain.vector_observation_space_size * brain.num_stacked_vector_observations
        if brain.vector_action_space_type == "continuous":
            activation_fn = tf.nn.tanh
        else:
            activation_fn = tf.nn.relu

        self.visual_in = []
        #for i in range(brain.number_visual_observations):
        #    height_size, width_size = brain.camera_resolutions[i]['height'], brain.camera_resolutions[i]['width']
        #    bw = brain.camera_resolutions[i]['blackAndWhite']
        #    visual_input = self.create_visual_input(height_size, width_size, bw, name="visual_observation_" + str(i))
        #    self.visual_in.append(visual_input)
        self.create_vector_input(s_size)

        final_hiddens = []
        for i in range(num_streams):
            visual_encoders = []
            hidden_state, hidden_visual = None, None
            #if brain.number_visual_observations > 0:
            #    for j in range(brain.number_visual_observations):
            #        encoded_visual = self.create_visual_encoder(self.visual_in[j], h_size, activation_fn, num_layers)
            #        visual_encoders.append(encoded_visual)
            #    hidden_visual = tf.concat(visual_encoders, axis=1)
            if brain.vector_observation_space_size > 0:
                s_size = brain.vector_observation_space_size * brain.num_stacked_vector_observations
                #if brain.vector_observation_space_type == "continuous":
                #    hidden_state = self.create_continuous_state_encoder(h_size, activation_fn, num_layers)
                #else:
                #    hidden_state = self.create_discrete_state_encoder(s_size, h_size,
                #                                                      activation_fn, num_layers)
                hidden_state = self.create_dummy_visual_encoder(self.vector_in, h_size, activation_fn, num_layers)
            if hidden_state is not None and hidden_visual is not None:
                final_hidden = tf.concat([hidden_visual, hidden_state], axis=1)
            elif hidden_state is None and hidden_visual is not None:
                final_hidden = hidden_visual
            elif hidden_state is not None and hidden_visual is None:
                final_hidden = hidden_state
            else:
                raise Exception("No valid network configuration possible. "
                                "There are no states or observations in this brain")
            final_hiddens.append(final_hidden)
        return final_hiddens
