import { Disclosure, DisclosureButton, DisclosurePanel } from "@headlessui/vue";
import { defineComponent, ref } from "vue";

export default defineComponent({
  setup() {
    const test = ref();
    const test2 = ref();

    function InsideTest() {
      console.log("test");
    }

    function CheckTestValue() {
      console.log("Test value: ", test.value);
    }

    return () => (
      <main>
        <Disclosure as="nav">
          {({ open, close }: { open: boolean; close: () => void }) => {
            test.value = open;
            test2.value = close;

            return (
              <>
                <DisclosureButton class="flex w-full justify-between rounded-lg bg-purple-100 px-4 py-2 text-left text-sm font-medium text-purple-900 hover:bg-purple-200 focus:outline-none focus-visible:ring focus-visible:ring-purple-500 focus-visible:ring-opacity-75">
                  <span>What is your refund policy?</span>
                </DisclosureButton>

                {InsideTest()}

                {/* IIFE */}
                {(() => {
                  return <div>sup playa</div>;
                })()}

                <DisclosurePanel
                  class={{ "px-4 pt-4 pb-2 text-sm text-gray-500": true }}
                >
                  If you're unhappy with your purchase for any reason, email us
                  within 90 days and we'll refund you in full, no questions
                  asked.
                </DisclosurePanel>
              </>
            );
          }}
        </Disclosure>

        <button
          class="bg-green-500 h-5 w-5"
          onClick={() => CheckTestValue()}
        ></button>
        <button
          class="bg-red-500 h-5 w-5"
          onClick={() => test2.value()}
        ></button>
      </main>
    );
  },
});
