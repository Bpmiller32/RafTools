import anime from "animejs/lib/anime.es.js";

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

// Empty animation
animations.EmptyAnimate = (el, done) => {
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
  anime({
    targets: el,
    duration: 500,
    height: "7rem",
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
  console.log("el: ", el);

  anime({
    targets: el,
    duration: 5000,
    opacity: [0, 0.99999],
    complete: () => {
      // el.removeAttribute("style");
      done?.();
    },
  });
};
// ButtonFill
animations.ButtonFillEnter = (el, done) => {
  let colortext = "rgb(255 255 255)";

  // if (props.args == "panelbutton") {
  //   colortext = "rgb(67 56 202)";
  // }

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

  // if (props.args == "panelbutton") {
  //   colortext = "rgb(67 56 202)";
  // } else {
  //   const refreshButton = el.childNodes[0];
  //   anime({
  //     targets: refreshButton,
  //     rotate: {
  //       value: "-=2turn",
  //       duration: 1800,
  //       easing: "easeInOutSine",
  //       loop: true,
  //     },
  //   });
  // }

  // const refreshButton = el.childNodes[0];
  // anime({
  //   targets: refreshButton,
  //   rotate: {
  //     value: "-=2turn",
  //     // duration: 1800,
  //     easing: "easeInOutSine",
  //     loop: true,
  //   },
  // });

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

animations.Spin = (el) => {
  return anime({
    targets: el,
    rotate: "-=2turn",
    easing: "easeInOutSine",
    loop: true,
    autoplay: false,
  });
};

export default animations;
