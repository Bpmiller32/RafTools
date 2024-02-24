import {
  PropType,
  TransitionGroup,
  defineComponent,
  onMounted,
  ref,
  watch,
} from "vue";
import anime from "animejs/lib/anime.es.js";
import BackEndModule from "../interfaces/BackEndModule";
import {
  StatusOnlineIcon,
  ArrowCircleDownIcon,
  ExclamationCircleIcon,
  RefreshIcon,
  XCircleIcon,
} from "@heroicons/vue/outline";
import ErrorLogo from "../assets/ErrorLogo.png";
import SmartMatchLogo from "../assets/SmartMatchLogo.png";
import ParascriptLogo from "../assets/ParascriptLogo.png";
import RoyalMailLogo from "../assets/RoyalMailLogo.png";

export default defineComponent({
  props: {
    name: String,
    module: Object as PropType<BackEndModule>,
  },
  setup(props) {
    // Animation refs setup
    const refreshIconRef = ref();
    let refreshIconAnimation: anime.AnimeInstance;

    const downloadButtonRef = ref();
    let downloadButtonFillAnimation: anime.AnimeInstance;
    let downloadButtonDrainAnimation: anime.AnimeInstance;

    const cancelButtonRef = ref();
    let cancelButtonEnterAnimation: anime.AnimeInstance;
    let cancelButtonLeaveAnimation: anime.AnimeInstance;

    // Mounting and watchers setup
    onMounted(() => {
      refreshIconAnimation = anime({
        targets: refreshIconRef.value,
        rotate: "-=2turn",
        easing: "easeInOutSine",
        loop: true,
        autoplay: false,
      });

      downloadButtonFillAnimation = anime({
        targets: downloadButtonRef.value,
        duration: 300,
        backgroundSize: ["0% 0%", "150% 150%"],
        width: ["8rem", "6.75rem"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      downloadButtonDrainAnimation = anime({
        targets: downloadButtonRef.value,
        duration: 300,
        backgroundSize: ["150% 150%", "0% 0%"],
        width: ["6.75rem", "8rem"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      cancelButtonEnterAnimation = anime({
        targets: cancelButtonRef.value,
        duration: 500,
        translateY: ["0.5rem", "0rem"],
        opacity: ["0", "0.9999"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      cancelButtonLeaveAnimation = anime({
        targets: cancelButtonRef.value,
        duration: 500,
        translateY: ["0rem", "0.5rem"],
        opacity: ["0.9999", "0"],
        easing: "easeInOutQuad",
        autoplay: false,
      });

      // First draw/mount tweaks
      if (props.module?.Status == 1) {
        refreshIconAnimation.play();

        downloadButtonRef.value.style.width = "8rem";
        downloadButtonRef.value.style.backgroundSize = "0% 0%";
      } else {
        refreshIconAnimation.pause();

        cancelButtonRef.value.style.opacity = "0";
      }
    });

    // Watch if status of the module changes
    watch(
      () => props.module?.Status,
      () => {
        if (props.module?.Status == 1) {
          refreshIconAnimation.play();
          downloadButtonDrainAnimation.play();
          cancelButtonEnterAnimation.play();
        } else {
          refreshIconAnimation.pause();
          downloadButtonFillAnimation.play();
          cancelButtonLeaveAnimation.play();
        }
      }
    );

    // Events
    function CrawlButtonClicked() {
      // Do nothing if Crawler is not in a ready state
      if (props.module?.Status != 0) {
        return;
      }

      // // PROD
      // // Define the request options
      // const requestOptions = {
      //   method: "POST",
      //   headers: {
      //     "Content-Type": "application/json",
      //   },
      //   body: JSON.stringify({
      //     moduleCommand: "start",
      //   }),
      // };

      // // Send the request using the Fetch API
      // fetch(
      //   "http://192.168.0.39:5000/" + props.name + "/crawler",
      //   requestOptions
      // ).then((response) => {
      //   if (!response.ok) {
      //     throw new Error("Network response was not ok");
      //   }
      // });

      // TEST
      // Define the request options
      const requestOptions = {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      };

      // Send the request using the Fetch API
      fetch("http://192.168.0.39:5000/toggle", requestOptions).then(
        (response) => {
          if (!response.ok) {
            throw new Error("Network response was not ok");
          }
        }
      );
    }

    function CancelButtonClicked() {
      // Do nothing if Crawler is not in an in progress state
      if (props.module?.Status != 1) {
        return;
      }

      // TEST
      // Define the request options
      const requestOptions = {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      };

      // Send the request using the Fetch API
      fetch("http://192.168.0.39:5000/toggle", requestOptions).then(
        (response) => {
          if (!response.ok) {
            throw new Error("Network response was not ok");
          }
        }
      );
    }

    // Subcomponents
    function StatusLabel() {
      return (
        <TransitionGroup
          enterFromClass="opacity-0 translate-y-[-0.25rem]"
          enterToClass="opacity-100"
          enterActiveClass="duration-[300ms]"
        >
          {() => {
            switch (props.module?.Status) {
              case 0:
                return (
                  <div
                    key="0"
                    class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-green-800 bg-green-100"
                  >
                    Ready
                  </div>
                );

              case 1:
                return (
                  <div
                    key="1"
                    class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-yellow-800 bg-yellow-100"
                  >
                    In Progress
                  </div>
                );

              default:
                return (
                  <div
                    key="default"
                    class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-red-800 bg-red-100"
                  >
                    Error
                  </div>
                );
            }
          }}
        </TransitionGroup>
      );
    }

    function StatusIcon() {
      return (
        <TransitionGroup
          enterFromClass="opacity-0 translate-y-[-0.5rem]"
          enterToClass="opacity-100"
          enterActiveClass="duration-[500ms]"
        >
          {() => {
            switch (props.module?.Status) {
              case 0:
                return (
                  <StatusOnlineIcon
                    key="0"
                    class="h-5 w-5 ml-1 text-green-500"
                  />
                );

              case 1:
                return (
                  <ArrowCircleDownIcon
                    key="1"
                    class="h-5 w-5 ml-1 text-yellow-500"
                  />
                );

              default:
                return (
                  <ExclamationCircleIcon
                    key="default"
                    class="h-5 w-5 ml-1 text-red-500"
                  />
                );
            }
          }}
        </TransitionGroup>
      );
    }

    function DirectoryImage() {
      switch (props.name) {
        case "SmartMatch":
          return (
            <img class="w-20 h-20 border rounded-full" src={SmartMatchLogo} />
          );
        case "Parascript":
          return (
            <img class="w-20 h-20 border rounded-full" src={ParascriptLogo} />
          );
        case "RoyalMail":
          return (
            <img class="w-20 h-20 border rounded-full" src={RoyalMailLogo} />
          );

        default:
          return <img class="w-20 h-20 border rounded-full" src={ErrorLogo} />;
      }
    }

    function DownloadButton() {
      return (
        <button
          ref={downloadButtonRef}
          onClick={CrawlButtonClicked}
          type="button"
          disabled={props.module?.Status == 0 ? false : true}
          class={{
            "cursor-not-allowed ": props.module?.Status != 0,
            "justify-self-center flex items-center px-2 py-2 max-h-8 bg-gradient-to-r bg-gray-500 from-indigo-600 to-indigo-600 hover:from-indigo-700 hover:to-indigo-700 bg-no-repeat bg-center border border-transparent text-sm text-white leading-4 font-medium rounded-md focus:outline-none":
              true,
          }}
        >
          <RefreshIcon
            ref={refreshIconRef}
            class="shrink-0 h-5 w-5 text-white z-10"
          />
          <TransitionGroup
            enterFromClass="opacity-0"
            enterToClass="opacity-100"
            enterActiveClass="duration-[1500ms]"
          >
            {() => {
              switch (props.module?.Status) {
                case 0:
                  return (
                    <p key="0" class="ml-1">
                      Download
                    </p>
                  );

                default:
                  return (
                    <p key="default" class="ml-1">
                      Downloading
                    </p>
                  );
              }
            }}
          </TransitionGroup>
        </button>
      );
    }

    function CancelButton() {
      return (
        <XCircleIcon
          ref={cancelButtonRef}
          onClick={CancelButtonClicked}
          class={{
            "select-none": props.module?.Status == 0,
            "cursor-pointer": props.module?.Status == 1,
            "mx-8 h-6 w-6 text-red-500": true,
          }}
        />
      );
    }

    // Render function
    return () => (
      <div class="overflow-hidden select-none min-w-[23rem] max-w-[23rem] min-h-[12rem] bg-white rounded-lg shadow divide-y divide-gray-200">
        <div class="flex items-center justify-between p-6">
          <div class="flex items-center">
            <p class="text-gray-900 text-sm font-medium">{props.name}</p>
            {StatusLabel()}
            {StatusIcon()}
          </div>
          {DirectoryImage()}
        </div>

        {/* <div class="flex min-h-[5rem] justify-between items-center"> */}
        <div class="min-h-[5rem] grid grid-cols-3 grid-rows-1 items-center">
          <div />
          {DownloadButton()}
          {CancelButton()}
        </div>
      </div>
    );
  },
});
