<script setup>
import anime from "animejs/lib/anime.es.js";

const props = defineProps({
  animation: { type: String, default: "Empty" },
  args: { type: null, required: false },
  transitionMode: { type: String, default: "out-in" },
});

// Transition handler
function Handler(el, done, state) {
  if (state == "enter") {
    const enterHandler = props.animation + "Enter";

    if (typeof animations[enterHandler] !== "undefined") {
      animations[enterHandler](el, done);
    } else {
      animations["EmptyEnter"](el, done);
    }

    return;
  } else if (state == "leave") {
    const leaveHandler = props.animation + "Leave";

    if (typeof animations[leaveHandler] !== "undefined") {
      animations[leaveHandler](el, done);
    } else {
      animations["EmptyLeave"](el, done);
    }

    return;
  }
}

// Animations object
const animations = {};

// KeyFrame translator
animations.CalcPercentages = (percentages, duration) => {
  let currPercent = 0;

  for (let index = 0; index < percentages.length; index++) {
    const prevPercent = currPercent;
    if (index > 0) {
      currPercent = percentages[index];
    }
    percentages[index] = (currPercent - prevPercent) * duration;
  }

  return percentages;
};

// ******************
// **  Animations  **
// ******************

// Empty animation
animations.EmptyEnter = (el, done) => {
  anime({
    targets: el,
    duration: 0,
    complete: done,
  });
};
animations.EmptyLeave = (el, done) => {
  anime({
    targets: el,
    duration: 0,
    complete: done,
  });
};

// HeadShake
animations.HeadShakeEnter = (el, done) => {
  const duration = 1000;
  const percentages = animations.CalcPercentages(
    [0, 0.065, 0.185, 0.315, 0.435, 0.5],
    duration
  );

  anime({
    targets: el,
    keyframes: [
      {
        translateX: 0,
        color: "rgb(239, 68, 68)",
        duration: percentages[0],
        easing: "easeInOutQuad",
      },
      {
        translateX: -6,
        rotateY: -9,
        duration: percentages[1],
        easing: "easeInOutQuad",
      },
      {
        translateX: 5,
        rotateY: 7,
        duration: percentages[2],
        easing: "easeInOutQuad",
      },
      {
        translateX: -3,
        rotateY: -5,
        duration: percentages[3],
        easing: "easeInOutQuad",
      },
      {
        translateX: 2,
        rotateY: 3,
        duration: percentages[4],
        easing: "easeInOutQuad",
      },
      {
        translateX: 0,
        duration: percentages[5],
        easing: "easeInOutQuad",
      },
    ],
    complete: done,
  });
};

// SlideDown
animations.SlideDownEnter = (el, done) => {
  let height = "7rem";

  if (props.args == "nav") {
    height = "9.65rem";
  }

  anime({
    targets: el,
    duration: 500,
    height: height,
    easing: "easeInOutQuad",
    complete: done,
  });
};
animations.SlideDownLeave = (el, done) => {
  anime({
    targets: el,
    duration: 500,
    height: "0rem",
    easing: "easeInOutQuad",
    complete: done,
  });
};

// Flash
animations.FlashEnter = (el, done) => {
  const duration = 1000;
  const percentages = animations.CalcPercentages(
    [0, 0.25, 0.5, 0.75, 1],
    duration
  );

  anime({
    targets: el,
    keyframes: [
      {
        opacity: 1,
        color: "rgb(34, 197, 94)",
        duration: percentages[0],
        easing: "easeInOutQuad",
      },
      {
        opacity: 0,
        duration: percentages[1],
        easing: "easeInOutQuad",
      },
      {
        opacity: 1,
        duration: percentages[2],
        easing: "easeInOutQuad",
      },
      {
        opacity: 0,
        duration: percentages[3],
        easing: "easeInOutQuad",
      },
      {
        opacity: 0.99999,
        duration: percentages[4],
        easing: "easeInOutQuad",
      },
    ],
    complete: done,
  });
};

// FadeIn
animations.FadeInEnter = (el, done) => {
  anime({
    targets: el,
    duration: 5000,
    opacity: [0, 0.99999],
    complete: () => {
      el.removeAttribute("style");
      done?.();
    },
  });
};

// Nav FadeIn
animations.NavFadeInEnter = (el, done) => {
  anime({
    targets: el,
    duration: 5000,
    // borderBottomColor: ["rgba(79, 70, 229, 0)", "rgba(79, 70, 229, 0.99999)"],
    borderBottomColor: ["rgb(209 213 219)", "rgb(79 70 229)"],
    complete: done,
  });
};

// Page Transitions
animations.FromRightToLeftEnter = (el, done) => {
  anime({
    targets: el,
    duration: 500,
    translateX: ["-100%", "0%"],
    easing: "easeInOutQuad",
    complete: done,
  });
};
animations.FromRightToLeftLeave = (el, done) => {
  anime({
    targets: el,
    duration: 500,
    translateX: ["0%", "100%"],
    easing: "easeInOutQuad",
    complete: done,
  });
};
animations.FromLeftToRightEnter = (el, done) => {
  anime({
    targets: el,
    duration: 500,
    translateX: ["100%", "0%"],
    easing: "easeInOutQuad",
    complete: done,
  });
};
animations.FromLeftToRightLeave = (el, done) => {
  anime({
    targets: el,
    duration: 500,
    translateX: ["0%", "-100%"],
    easing: "easeInOutQuad",
    complete: done,
  });
};

// ButtonFill
animations.ButtonFillEnter = (el, done) => {
  let colortext = "rgb(255 255 255)";

  if (props.args == "panelbutton") {
    colortext = "rgb(67 56 202)";
  }

  anime({
    targets: el,
    keyframes: [
      { duration: 0, backgroundColor: "rgb(107 114 128)" },
      {
        duration: 300,
        color: ["rgb(255 255 255)", colortext],
        backgroundSize: ["0% 0%", "150% 150%"],
        easing: "easeInOutQuad",
      },
    ],
    complete: () => {
      el.removeAttribute("style");
      done?.();
    },
  });
};

// ButtonDrain
animations.ButtonDrainEnter = (el, done) => {
  let colortext = "rgb(255 255 255)";

  if (props.args == "panelbutton") {
    colortext = "rgb(67 56 202)";
  }

  anime({
    targets: el,
    duration: 300,
    color: [colortext, "rgb(255 255 255)"],
    backgroundSize: ["150% 150%", "0% 0%"],
    easing: "easeInOutQuad",
    complete: () => {
      el.removeAttribute("style");
      done?.();
    },
  });
};
</script>

<template>
  <Transition
    :mode="props.transitionMode"
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
