#include "plugin.hpp"

struct Rs2 : URack::UModule {
	enum ParamIds {
		NUM_PARAMS
	};
	enum InputIds {
		NUM_INPUTS
	};
	enum OutputIds {
		POINT_CLOUD_OUTPUT,
		NUM_OUTPUTS
	};
	enum LightIds {
		ACTIVE_LIGHT,
		NUM_LIGHTS
	};

	Rs2() {
		config(NUM_PARAMS, NUM_INPUTS, NUM_OUTPUTS, NUM_LIGHTS);
	}

	void update(const ProcessArgs& args) override {
	}
};


struct Rs2Widget : URack::UModuleWidget {
	Rs2Widget(Rs2* module) {
		setModule(module);
		setPanel(APP->window->loadSvg(asset::plugin(pluginInstance, "res/Rs2.svg")));

		addChild(createWidget<ScrewBlack>(Vec(RACK_GRID_WIDTH, 0)));
		addChild(createWidget<ScrewBlack>(Vec(box.size.x - 2 * RACK_GRID_WIDTH, 0)));
		addChild(createWidget<ScrewBlack>(Vec(RACK_GRID_WIDTH, RACK_GRID_HEIGHT - RACK_GRID_WIDTH)));
		addChild(createWidget<ScrewBlack>(Vec(box.size.x - 2 * RACK_GRID_WIDTH,	RACK_GRID_HEIGHT - RACK_GRID_WIDTH)));

		addPointCloudOutput(mm2px(Vec(23.848, 111.213)), module, Rs2::POINT_CLOUD_OUTPUT, "PointCloudOutput");
	}
};


Model* modelRs2 = createModel<Rs2, Rs2Widget>("Rs2");
