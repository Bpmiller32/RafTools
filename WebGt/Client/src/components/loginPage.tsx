import { defineComponent, onMounted, ref } from "vue";
import volarisLogo from "../assets/volarisLogo.svg";
import axios from "axios";

export default defineComponent({
  props: {
    apiUrl: {
      type: String,
      required: true,
    },
  },
  emits: { appStarted: () => true }, // No payload, just the event
  setup(props, { emit }) {
    /* -------------------------------------------------------------------------- */
    /*                          Component state and setup                         */
    /* -------------------------------------------------------------------------- */
    const isServerOnline = ref(false);

    onMounted(async () => {
      try {
        await axios.get(props.apiUrl + "/helloWorld");

        isServerOnline.value = true;
      } catch {
        console.log("Server not available");
        isServerOnline.value = false;
      }
    });

    /* -------------------------------------------------------------------------- */
    /*                                   Events                                   */
    /* -------------------------------------------------------------------------- */
    const StartAppButtonClicked = () => {
      emit("appStarted");
    };

    /* -------------------------------------------------------------------------- */
    /*                                Subcomponents                               */
    /* -------------------------------------------------------------------------- */
    const ServerStatusBadge = () => {
      return (
        <span class="mb-2 inline-flex items-center gap-x-1.5 rounded-full px-2 py-1 text-xs font-medium text-gray-100 ring-1 ring-inset ring-gray-200">
          {ServerStatusBadgeIconSelector()}
          Server status
        </span>
      );
    };

    const ServerStatusBadgeIconSelector = () => {
      if (isServerOnline.value) {
        return (
          <svg
            class="h-1.5 w-1.5 fill-green-500"
            viewBox="0 0 6 6"
            aria-hidden="true"
          >
            <circle cx="3" cy="3" r="3" />
          </svg>
        );
      } else {
        return (
          <svg
            class="h-1.5 w-1.5 fill-red-500"
            viewBox="0 0 6 6"
            aria-hidden="true"
          >
            <circle cx="3" cy="3" r="3" />
          </svg>
        );
      }
    };

    const StartAppButton = () => {
      if (isServerOnline.value) {
        return (
          <button
            type="button"
            onClick={() => StartAppButtonClicked()}
            class="rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 duration-300"
          >
            Start App
          </button>
        );
      } else {
        return (
          <button
            type="button"
            class="cursor-not-allowed rounded-md bg-gray-600 px-3 py-2 text-sm font-semibold text-white shadow-sm"
          >
            Start App
          </button>
        );
      }
    };

    /* -------------------------------------------------------------------------- */
    /*                               Render function                              */
    /* -------------------------------------------------------------------------- */
    return () => (
      <main class="w-screen h-screen flex justify-center items-center">
        <section>
          {/* App logo */}
          <div class="mb-5">
            <img src={volarisLogo} class="h-5 w-full" />
          </div>

          {/* Server status */}
          <div class="flex justify-end">{ServerStatusBadge()}</div>

          {/* Username and pass input fields */}
          <div class="isolate -space-y-px rounded-md shadow-sm">
            <div class="relative rounded-md rounded-b-none px-3 pb-1.5 pt-2.5 ring-1 ring-inset ring-gray-300 focus-within:z-10 focus-within:ring-2 focus-within:ring-indigo-600 duration-300">
              <label for="name" class="block text-xs font-medium text-gray-100">
                Username
              </label>
              <input
                type="text"
                class="block bg-[#211d20] w-full border-0 p-0 text-gray-100 placeholder:text-gray-400 focus:ring-0"
                placeholder="billym"
              />
            </div>
            <div class="relative rounded-md rounded-t-none px-3 pb-1.5 pt-2.5 ring-1 ring-inset ring-gray-300 focus-within:z-10 focus-within:ring-2 focus-within:ring-indigo-600 duration-300">
              <label
                for="job-title"
                class="block text-xs font-medium text-gray-100"
              >
                Password
              </label>
              <input
                type="password"
                class="block bg-[#211d20] w-full border-0 p-0 text-gray-100 placeholder:text-gray-400 focus:ring-0"
                placeholder="**********"
              />
            </div>
          </div>

          {/* Start app button */}
          <div class="flex justify-center mt-2">{StartAppButton()}</div>
        </section>
      </main>
    );
  },
});
