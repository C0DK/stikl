module Stikl.Web.Components.Modal

let render title content =
    // TODO: use basic html modal
    // TODO: handle click on overlay..
    $"""
<div
    id="modal"
    role="dialog"
    tabindex="-1"
    _="on closeModal remove me"
    class="fixed z-50 inset-0 bg-gray-900 bg-opacity-60 overflow-y-auto h-full w-full px-4"
    >
    <div class="relative top-40 mx-auto shadow-xl rounded-md bg-white max-w-md">
        <div class="relative bg-white rounded-lg shadow">
            <div class="flex justify-end p-2">
                <h1 class="font-sans text-xl">
                    {title}
                </h1>
                <button
                    _="on click trigger closeModal"
                    type="button"
                    class="text-gray-400 bg-transparent hover:bg-gray-200 hover:text-gray-900 rounded-lg text-sm p-1.5 ml-auto inline-flex items-center"
                    >
                    <i class="fa-solid fa-xmark"></i>
                </button>
            </div>
            {content}
	    </div>
	</div>
</div>
"""


