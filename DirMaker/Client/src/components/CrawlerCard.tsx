import {
  PropType,
  Transition,
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
    let mountedOnce = false;

    const refreshIconRef = ref();
    const downloadButtonRef = ref();

    let refreshIconAnimation: anime.AnimeInstance;
    let downloadButtonFillAnimation: anime.AnimeInstance;
    let downloadButtonDrainAnimation: anime.AnimeInstance;

    // Mounting and watchers setup
    onMounted(() => {
      mountedOnce = true;

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

      if (props.module?.Status == 1) {
        refreshIconAnimation.play();
        downloadButtonRef.value.style.width = "8rem";
        downloadButtonRef.value.style.backgroundSize = "0% 0%";
      } else if (props.module?.Status == 1) {
        downloadButtonRef.value.style.width = "8rem";
        downloadButtonRef.value.style.backgroundSize = "0% 0%";
      } else {
        refreshIconAnimation.pause();
      }
    });

    watch(
      () => props.module?.Status,
      () => {
        if (props.module?.Status == 1) {
          refreshIconAnimation.play();
          downloadButtonDrainAnimation.play();
        } else {
          refreshIconAnimation.pause();
          downloadButtonFillAnimation.play();
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
      // fetch("http://192.168.0.39:5000/smartmatch/crawler", requestOptions).then(
      //   (response) => {
      //     if (!response.ok) {
      //       throw new Error("Network response was not ok");
      //     }
      //   }
      // );

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

      // // PROD
      // // Define the request options
      // const requestOptions = {
      //   method: "POST",
      //   headers: {
      //     "Content-Type": "application/json",
      //   },
      //   body: JSON.stringify({
      //     moduleCommand: "stop",
      //   }),
      // };

      // // Send the request using the Fetch API
      // fetch("http://192.168.0.39:5000/smartmatch/crawler", requestOptions).then(
      //   (response) => {
      //     if (!response.ok) {
      //       throw new Error("Network response was not ok");
      //     }
      //   }
      // );
    }

    // Subcomponents
    function StatusLabel() {
      // Needed to make one element different from the others so that the Vue transition system does not resuse the div
      // If all elements have the key then the divs are again identical and reuseable
      const statusLabelKey = 0;

      switch (props.module?.Status) {
        case 0:
          return (
            <Transition
              mode="out-in"
              enterFromClass="opacity-0 translate-y-[-0.25rem]"
              enterToClass="opacity-100"
              enterActiveClass="duration-[300ms]"
            >
              <div
                key={statusLabelKey}
                class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-green-800 bg-green-100"
              >
                Ready
              </div>
            </Transition>
          );

        case 1:
          return (
            <Transition
              mode="out-in"
              enterFromClass="opacity-0 translate-y-[-0.25rem]"
              enterToClass="opacity-100"
              enterActiveClass="duration-[300ms]"
            >
              <div class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-yellow-800 bg-yellow-100">
                In Progress
              </div>
            </Transition>
          );

        case 2:
          return (
            <Transition
              mode="out-in"
              enterFromClass="opacity-0 translate-y-[-0.25rem]"
              enterToClass="opacity-100"
              enterActiveClass="duration-[300ms]"
            >
              <div class="ml-3 px-2 py-0.5 text-xs font-medium rounded-full text-red-800 bg-red-100">
                Error
              </div>
            </Transition>
          );
      }
    }

    function StatusIcon() {
      switch (props.module?.Status) {
        case 0:
          return (
            <Transition
              mode="out-in"
              enterFromClass="opacity-0 translate-y-[-0.5rem]"
              enterToClass="opacity-100"
              enterActiveClass="duration-[500ms]"
            >
              <StatusOnlineIcon class="h-5 w-5 ml-1 text-green-500" />;
            </Transition>
          );

        case 1:
          return (
            <Transition
              mode="out-in"
              enterFromClass="opacity-0 translate-y-[-0.5rem]"
              enterToClass="opacity-100"
              enterActiveClass="duration-[500ms]"
            >
              <ArrowCircleDownIcon class="h-5 w-5 ml-1 text-yellow-500" />;
            </Transition>
          );

        case 2:
          return (
            <Transition
              mode="out-in"
              enterFromClass="opacity-0 translate-y-[-0.5rem]"
              enterToClass="opacity-100"
              enterActiveClass="duration-[500ms]"
            >
              <ExclamationCircleIcon class="h-5 w-5 ml-1 text-red-500" />;
            </Transition>
          );
      }
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
            "flex items-center my-4 px-2 py-2 max-h-8 bg-gradient-to-r bg-gray-500 from-indigo-600 to-indigo-600 hover:from-indigo-700 hover:to-indigo-700 bg-no-repeat bg-center border border-transparent text-sm text-white leading-4 font-medium rounded-md focus:outline-none":
              true,
          }}
        >
          <RefreshIcon
            ref={refreshIconRef}
            class="shrink-0 h-5 w-5 text-white z-10"
          />
          {DownloadButtonTextHelper()}
        </button>
      );
    }

    function CancelButton() {
      const cancelButtonKey = 0;

      if (props.module?.Status == 1) {
        return (
          <Transition
            mode="out-in"
            appear={mountedOnce}
            enterFromClass="opacity-0 translate-y-[0.5rem]"
            enterToClass="opacity-100"
            enterActiveClass="duration-[1000ms]"
          >
            <XCircleIcon
              key={cancelButtonKey}
              onClick={CancelButtonClicked}
              class="cursor-pointer mr-16 h-6 w-6 text-red-500"
            ></XCircleIcon>
          </Transition>
        );
      } else {
        return (
          <Transition
            mode="out-in"
            appear={mountedOnce}
            enterFromClass="opacity-100"
            enterToClass="opacity-0"
            enterActiveClass="duration-[500ms]"
          >
            <XCircleIcon
              onClick={CancelButtonClicked}
              class="mr-16 h-6 w-6 text-red-500 opacity-100 text-opacity-0"
            ></XCircleIcon>
          </Transition>
        );
      }
    }

    function DownloadButtonTextHelper() {
      const downloadButtonKey = 0;

      switch (props.module?.Status) {
        case 0:
          return (
            <Transition
              mode="out-in"
              enterFromClass="opacity-0"
              enterToClass="opacity-100"
              enterActiveClass="duration-[1500ms]"
            >
              <p key={downloadButtonKey} class="ml-1">
                Download
              </p>
              ;
            </Transition>
          );
        case 1:
          return (
            <Transition
              mode="out-in"
              enterFromClass="opacity-0"
              enterToClass="opacity-100"
              enterActiveClass="duration-[1500ms]"
            >
              <p class="ml-1">Downloading</p>;
            </Transition>
          );
        case 2:
          return (
            <Transition
              mode="out-in"
              enterFromClass="opacity-0"
              enterToClass="opacity-100"
              enterActiveClass="duration-[1500ms]"
            >
              <p class="ml-1">Downloading</p>;
            </Transition>
          );
      }
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

        <div class="flex min-h-[5rem] justify-between items-center">
          <div class="ml-16 w-6 h-6"></div>
          {DownloadButton()}
          {CancelButton()}
        </div>
      </div>
    );
  },
});
