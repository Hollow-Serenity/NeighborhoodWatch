// Author: https://dev.to/robinvanderknaap/setting-up-bootstrap-4-sass-with-aspnet-core-4ff6
const path = require('path');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const webpack = require("webpack");
const {CleanWebpackPlugin} = require('clean-webpack-plugin');

const bundleFileName = 'bundle';
const dirName = 'wwwroot/dist';

module.exports = (env, argv) => {
    return {
        mode: argv.mode === "production" ? "production" : "development",
        entry: ['./node_modules/jquery/dist/jquery.js', './node_modules/bootstrap/dist/js/bootstrap.bundle.js', './wwwroot/js/site.js', './wwwroot/scss/site.scss'],
        output: {
            filename: bundleFileName + (argv.mode === "production" ? '.min' : '') + '.js',
            path: path.resolve(__dirname, dirName)
        },
        module: {
            rules: [
                {
                    test: /\.s[c|a]ss$/,
                    use:
                        [
                            'style-loader',
                            MiniCssExtractPlugin.loader,
                            'css-loader',
                            {
                                loader: 'postcss-loader', // Run postcss actions
                                options: {
                                    postcssOptions: {
                                        plugins: function () { // postcss plugins, can be exported to postcss.config.js

                                            let plugins = [require('autoprefixer')];

                                            if (argv.mode === "production") {

                                                plugins.push(require('cssnano'));
                                            }

                                            return plugins;
                                        }
                                    }
                                }
                            },
                            'sass-loader'
                        ]
                }
            ]
        },
        plugins: [
            /* Disable the removal of output dir as we need both minified and non-minified files */
            //new CleanWebpackPlugin(),
            new MiniCssExtractPlugin({
                filename: bundleFileName + (argv.mode === "production" ? '.min' : '') + '.css'
            }),
            new webpack.ProvidePlugin({
                $: "jquery",
                jQuery: "jquery"
            })
        ]
    };
};