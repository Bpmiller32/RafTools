<script setup>
import animations from "../animations.js";

const props = defineProps(["animation", "args"]);

// Transition handler
function Handler(el, done, state) {
  if (state == "enter") {
    const enterHandler = props.animation + "Enter";
    if (typeof animations[enterHandler] !== "undefined") {
      animations[enterHandler](el, done);
    } else {
      animations["EmptyAnimate"](el, done);
    }

    return;
  }
  if (state == "leave") {
    const leaveHandler = props.animation + "Leave";
    if (typeof animations[leaveHandler] !== "undefined") {
      animations[leaveHandler](el, done);
    } else {
      animations["EmptyAnimate"](el, done);
    }

    return;
  }
}
</script>

<template>
  <Transition
    mode="out-in"
    :css="false"
    @enter="
      (el, done) => {
        Handler(el, done, 'enter');
      }
    "
    @leave="
      (el, done) => {
        Handler(el, done, 'leave');
      }
    "
  >
    <slot></slot>
  </Transition>
</template>
