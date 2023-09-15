import { Popover, PopoverButton, PopoverPanel } from "@headlessui/vue";
import { defineComponent, Transition } from "vue";
import { useRoute } from "vue-router";
import matwLogoSmall from "../assets/matwLogoSmall.png";
import matwLogoLarge from "../assets/matwLogoLarge.png";

export default defineComponent({
  setup() {
    const route = useRoute();

    let renderedOnce = false;

    let isPanelOpen: boolean;
    let ClosePanel: () => void;

    // Multiuse
    function NavLinkDesktop(linkName: string) {
      return (
        <Transition
          appear
          enterFromClass="border-b-transparent"
          enterToClass={
            route.name == linkName
              ? "border-b-indigo-600"
              : "border-b-transparent"
          }
          enterActiveClass="duration-[1000ms]"
        >
          <router-link
            to={`/${linkName}`}
            class={{
              "hidden sm:flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2":
                true,
              "border-indigo-600": route.name == linkName,
              "border-transparent hover:border-gray-300 transition-border duration-500":
                route.name != linkName,
            }}
          >
            {linkName}
          </router-link>
        </Transition>
      );
    }

    function NavLinkMobile(linkName: string) {
      return (
        <router-link
          to={`/${linkName}`}
          onClick={() => ClosePanel()}
          class={{
            "block border-l-4 p-3 text-base font-medium": true,
            "border-indigo-600 bg-indigo-50": route.name == linkName,
            "border-gray-300 bg-white hover:bg-gray-100 transition-background-color duration-500":
              route.name != linkName,
          }}
        >
          {linkName}
        </router-link>
      );
    }

    // Organization
    function NavLogo() {
      return (
        <div class="flex items-center">
          <img class="hidden sm:block h-8 w-[228px]" src={matwLogoLarge} />
          <img class="block sm:hidden h-8" src={matwLogoSmall} />
        </div>
      );
    }

    function MobileButton() {
      return (
        <PopoverButton
          as="button"
          class="block sm:hidden p-2 bg-white rounded-md text-gray-400 hover:text-gray-500 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-background-color transition-color duration-500"
        >
          {isPanelOpen ? <div>OpenButton</div> : <div>ClosedButton</div>}
        </PopoverButton>
      );
    }

    function MobilePanel() {
      return (
        <>
          {isPanelOpen ? (
            <Transition
              appear
              enterFromClass="h-0"
              enterToClass="h-[9.65rem]"
              enterActiveClass="duration-[750ms]"
            >
              <PopoverPanel
                as="nav"
                class="block sm:hidden space-y-1 overflow-hidden focus:outline-none"
              >
                {NavLinkMobile("Download")}
                {NavLinkMobile("Build")}
                {NavLinkMobile("Test")}
              </PopoverPanel>
            </Transition>
          ) : (
            <Transition
              appear
              mode="out-in"
              enterFromClass="h-[9.65rem]"
              enterToClass="h-0"
              enterActiveClass="duration-[750ms]"
              onBeforeEnter={(el: Element) => {
                if (!renderedOnce) {
                  el.setAttribute("style", "display:none");
                  renderedOnce = true;
                }
              }}
              onAfterEnter={(el: Element) => {
                el.setAttribute("style", "display:none");
              }}
            >
              <div class="block sm:hidden space-y-1 border-t-2 overflow-hidden focus:outline-none">
                {NavLinkMobile("Download")}
                {NavLinkMobile("Build")}
                {NavLinkMobile("Test")}
              </div>
            </Transition>
          )}
        </>
      );
    }

    return () => {
      if (typeof route.name === "undefined") {
        return;
      }

      return (
        <Popover as="nav" class="bg-white shadow-sm select-none">
          {({ open, close }: { open: boolean; close: () => void }) => {
            // Assign render props to component variables
            isPanelOpen = open;
            ClosePanel = close;

            return (
              <>
                <div class="flex justify-between h-16 px-4">
                  <div class="flex space-x-8">
                    {NavLogo()}
                    {NavLinkDesktop("Download")}
                    {NavLinkDesktop("Build")}
                    {NavLinkDesktop("Test")}
                  </div>
                  <div class="flex items-center">{MobileButton()}</div>
                </div>
                {MobilePanel()}
              </>
            );
          }}
        </Popover>
      );
    };
  },
});
