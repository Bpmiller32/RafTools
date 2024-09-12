import { CheckCircleIcon } from "@heroicons/vue/16/solid";
import { defineComponent, onMounted, ref, Transition } from "vue";
import Emitter from "../webgl/utils/eventEmitter";

export default defineComponent({
  setup() {
    /* ---------------------------------- State --------------------------------- */
    const isAlertEnabled = ref(false);
    const statusAlertText = ref();

    onMounted(() => {
      // TODO: fix this, experience and therefore events firing are not ready by the time this mounts
      setTimeout(() => {
        Emitter.on("fillInForm", () => {
          statusAlertText.value = "Successfully uploaded";
          isAlertEnabled.value = true;
        });
        Emitter.on("gotoNextImage", () => {
          statusAlertText.value = "Loading next image....";
          isAlertEnabled.value = true;
        });

        Emitter.on("loadedFromApi", () => {
          isAlertEnabled.value = false;
        });
      }, 1000);
    });

    /* ------------------------------ Subcomponents ----------------------------- */

    /* ----------------------------- Render function ---------------------------- */
    return () => (
      <Transition
        enterFromClass="opacity-0 -translate-y-full"
        enterToClass="opacity-100 translate-y-0"
        enterActiveClass="transition duration-[500ms]"
        leaveFromClass="opacity-100 translate-y-0"
        leaveToClass="opacity-0 -translate-y-full"
        leaveActiveClass="transition duration-[500ms]"
      >
        {() => {
          if (isAlertEnabled.value) {
            return (
              <article class="mt-4 max-w-60">
                <div class="rounded-md bg-green-50 p-3">
                  <div class="flex">
                    <div class="flex-shrink-0">
                      <CheckCircleIcon class="h-5 w-5 text-green-400" />
                    </div>
                    <div class="ml-3">
                      <p class="text-sm font-medium text-green-800">
                        {statusAlertText.value}
                      </p>
                    </div>
                  </div>
                </div>
              </article>
            );
          }
        }}
      </Transition>
    );
  },
});
